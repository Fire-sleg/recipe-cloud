using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using RecipeService.Models;
using RecipeService.Models.Categories.DTOs;
using RecipeService.Models.Filter;
using RecipeService.Models.Pagination;
using RecipeService.Models.Recipes;
using RecipeService.Models.Recipes.DTOs;
using RecipeService.Repository;
using RecipeService.Services;
using StackExchange.Redis;
using System.Net;
using System.Security.Claims;
using IDatabase = StackExchange.Redis.IDatabase;

namespace RecipeService.Controllers
{
    [Route("api/recipes")]
    [ApiController]
    public class RecipeController : ControllerBase
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IMapper _mapper;
        private readonly IMinIOService _minIOService;
        private readonly IRedisCache _redis;
        private readonly IValidator<RecipeCreateDTO> _createValidator;
        private readonly IValidator<(RecipeUpdateDTO, IFormFile)> _updateValidator;
        private readonly ILogger<RecipeController> _logger;

        public RecipeController(
            IRecipeRepository recipeRepository,
            IMapper mapper,
            IMinIOService minIOService,
            IRedisCache redis,
            IValidator<RecipeCreateDTO> createValidator,
            IValidator<(RecipeUpdateDTO, IFormFile)> updateValidator,
            ILogger<RecipeController> logger)
        {
            _recipeRepository = recipeRepository;
            _mapper = mapper;
            _minIOService = minIOService;
            _redis = redis;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _logger = logger;
        }

        #region Helpers
        private bool TryGetUserId(out Guid userId)
        {
            var userIdClaim = User.FindFirst("ident")?.Value;
            return Guid.TryParse(userIdClaim, out userId);
        }
        /// <summary>
        /// Parse filters from query string to DTO.
        /// TODO Test lists (Diets, etc), lists can be null, RecipeFilterDto
        /// </summary>
        private RecipeFilterDTO ParseFilters(string filters)
        {
            _logger.LogDebug("Parsing filters: {Filters}", filters);

            var filterDto = new RecipeFilterDTO();
            var filterSegments = filters.Split('&', StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in filterSegments)
            {
                var keyValue = segment.Split('=');
                if (keyValue.Length != 2) continue;

                var key = keyValue[0];
                var value = keyValue[1];

                switch (key)
                {
                    case "title": filterDto.Title = value; break;
                    case "diets": filterDto.Diets.Add(value); break;
                    case "allergens": filterDto.Allergens.Add(value); break;
                    case "tags": filterDto.Tags.Add(value); break;
                    case "cuisines": filterDto.Cuisines.Add(value); break;
                    case "isUserCreated": filterDto.IsUserCreated = bool.Parse(value); break;
                    case "categoryId": if (Guid.TryParse(value, out var guid)) filterDto.CategoryId = guid; break;
                }
            }

            _logger.LogDebug("Parsed filter DTO result: {@FilterDto}", filterDto);
            return filterDto;
        }
        #endregion

        /// <summary>
        /// Get total number of recipes
        /// </summary>
        [HttpGet("count")]
        [EnableRateLimiting("basic")]
        [ProducesResponseType(typeof(APIResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status500InternalServerError)]
        [ProducesDefaultResponseType(typeof(APIResponse<object>))]
        public async Task<ActionResult<APIResponse<int>>> GetRecipesCountAsync(CancellationToken ct)
        {
            var key = $"recipe:count";
            _logger.LogInformation("Attempting to fetch recipe count from cache with key: {Key}.", key);

            var cachedCount = await _redis.GetAsync<int?>(key);
            if (cachedCount.HasValue)
            {
                _logger.LogInformation("Recipes count retrieved from cache.");
                return APIResponse<int>.Success(cachedCount.Value);
            }

            _logger.LogInformation("Fetching total recipe count");
            var count = await _recipeRepository.CountAsync(cancellationToken: ct);

            _logger.LogDebug("Fetched {Count} recipes", count);

            await _redis.SetAsync(key, count, TimeSpan.FromHours(24));
            _logger.LogInformation("Recipe count cached successfully.");

            return APIResponse<int>.Success(count);
        }

        /// <summary>
        /// Get recipe by ID
        /// </summary>
        [HttpGet("{id:guid}", Name = "GetRecipe")]
        public async Task<ActionResult<APIResponse<RecipeDTO>>> GetRecipeByIdAsync(Guid id, CancellationToken ct)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("Invalid recipe ID provided");
                return APIResponse<RecipeDTO>.Fail("Invalid ID", HttpStatusCode.BadRequest);
            }

            var cacheKey = $"recipe:{id}";
            _logger.LogInformation("Attempting to fetch recipe from cache with key: {Key}.", cacheKey);

            var cachedRecipe = await _redis.GetAsync<RecipeDTO>(cacheKey);
            if (cachedRecipe != null)
            {
                _logger.LogInformation("Recipe retrieved from cache.");
                return APIResponse<RecipeDTO>.Success(cachedRecipe);
            }

            _logger.LogInformation("Fetching recipe with ID {Id} from DB", id);
            var recipe = await _recipeRepository.GetAsync(r => r.Id == id, cancellationToken: ct);

            if (recipe is null)
            {
                _logger.LogWarning("Recipe {Id} not found", id);
                return APIResponse<RecipeDTO>.Fail("Recipe not found", HttpStatusCode.NotFound);
            }

            var dto = _mapper.Map<RecipeDTO>(recipe);

            await _redis.SetAsync(cacheKey, dto, TimeSpan.FromHours(24));
            _logger.LogInformation("Recipe cached successfully.");

            return APIResponse<RecipeDTO>.Success(dto);
        }

        /// <summary>
        /// Get paged list of recipes.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<APIResponse<PagedResponse<RecipeDTO>>>> GetRecipesAsync(
            [FromQuery] PaginationParams paginationParams,
            CancellationToken ct = default)
        {
            var cacheKey = $"recipes:page:{paginationParams.PageNumber}:size:{paginationParams.PageSize}";
            _logger.LogInformation("Attempting to fetch recipes page from cache with key: {Key}.", cacheKey);

            var cachedPage = await _redis.GetAsync<PagedResponse<RecipeDTO>>(cacheKey);
            if (cachedPage != null)
            {
                _logger.LogInformation("Paged recipes retrieved from cache.");
                return APIResponse<PagedResponse<RecipeDTO>>.Success(cachedPage);
            }

            _logger.LogInformation("Fetching recipes page {Page}, size {Size} from DB",
                paginationParams.PageNumber, paginationParams.PageSize);

            var list = await _recipeRepository.GetAllAsync(filter: null, paginationParams, ct);
            var recipes = _mapper.Map<List<RecipeDTO>>(list);

            var totalCount = await _recipeRepository.CountAsync(cancellationToken: ct);

            var pagedResponse = new PagedResponse<RecipeDTO>(
                recipes,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);

            await _redis.SetAsync(cacheKey, pagedResponse, TimeSpan.FromHours(24));
            _logger.LogInformation("Paged recipes cached successfully.");

            return APIResponse<PagedResponse<RecipeDTO>>.Success(pagedResponse);
        }

        /// <summary>
        /// Get recipe by transliterated name.
        /// </summary>
        [HttpGet("by-slug/{transliteratedName}")]
        public async Task<ActionResult<APIResponse<RecipeDTO>>> GetByTransliteratedNameAsync(
            [FromRoute] string transliteratedName,
            CancellationToken ct = default)
        {
            //var cacheKey = $"recipe:slug:{transliteratedName}";
            //_logger.LogInformation("Attempting to fetch recipe by slug from cache with key: {Key}.", cacheKey);

            //var cachedRecipe = await _redis.GetAsync<RecipeDTO>(cacheKey);
            //if (cachedRecipe != null)
            //{
            //    _logger.LogInformation("Recipe by slug retrieved from cache.");
            //    return APIResponse<RecipeDTO>.Success(cachedRecipe);
            //}

            _logger.LogInformation("Fetching recipe by transliteratedName: {Slug} from DB", transliteratedName);
            var recipe = await _recipeRepository.GetAsync(u => u.TransliteratedName == transliteratedName, cancellationToken: ct);

            if (recipe is null)
            {
                _logger.LogWarning("Recipe not found by slug: {Slug}", transliteratedName);
                return APIResponse<RecipeDTO>.Fail("Recipe not found", HttpStatusCode.NotFound);
            }

            var dto = _mapper.Map<RecipeDTO>(recipe);
            //await _redis.SetAsync(cacheKey, dto, TimeSpan.FromHours(24));
            _logger.LogInformation("Recipe by slug cached successfully.");

            return APIResponse<RecipeDTO>.Success(dto);
        }

        /// <summary>
        /// Get all recipes created by specific user.
        /// </summary>
        [Authorize]
        [HttpGet("user/{userId:guid}")]
        public async Task<ActionResult<APIResponse<List<RecipeDTO>>>> GetByUserIdAsync(
            [FromRoute] Guid userId,
            CancellationToken ct = default)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Invalid userId provided");
                return APIResponse<List<RecipeDTO>>.Fail("Invalid user ID", HttpStatusCode.BadRequest);
            }

            var cacheKey = $"recipes:user:{userId}";
            _logger.LogInformation("Attempting to fetch user's recipes from cache with key: {Key}.", cacheKey);

            var cachedRecipes = await _redis.GetAsync<List<RecipeDTO>>(cacheKey);
            if (cachedRecipes != null)
            {
                _logger.LogInformation("User's recipes retrieved from cache.");
                return APIResponse<List<RecipeDTO>>.Success(cachedRecipes);
            }

            _logger.LogInformation("Fetching recipes by userId: {UserId} from DB", userId);
            var list = await _recipeRepository.GetAllAsync(u => u.CreatedBy == userId, cancellationToken: ct);

            var recipes = _mapper.Map<List<RecipeDTO>>(list);

            await _redis.SetAsync(cacheKey, recipes, TimeSpan.FromHours(24));
            _logger.LogInformation("User's recipes cached successfully.");

            return APIResponse<List<RecipeDTO>>.Success(recipes);
        }



        /// <summary>
        /// Create a new recipe, dto contains image.
        /// </summary>
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(APIResponse<RecipeDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse<RecipeDTO>>> CreateRecipeAsync(
            [FromForm] RecipeCreateDTO createDTO,
            CancellationToken ct = default)
        {
            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("CreateRecipe rejected: no user id in claims");
                return APIResponse<RecipeDTO>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
            }

            // Validate input (DTO + image)
            var validation = await _createValidator.ValidateAsync(createDTO, ct);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("CreateRecipe validation failed: {Errors}", string.Join("; ", errors));
                return APIResponse<RecipeDTO>.Fail(errors, HttpStatusCode.BadRequest);
            }

            // Upload image if provided (validator may enforce non-null)
            string? imageUrl = null;
            if (createDTO.Image is not null && createDTO.Image.Length > 0)
            {
                _logger.LogInformation("Uploading recipe image...");
                imageUrl = await _minIOService.UploadImageAsync(createDTO.Image, ct);
            }

            var recipe = _mapper.Map<Recipe>(createDTO);
            recipe.ImageUrl = imageUrl;
            recipe.CreatedBy = userId;
            recipe.CreatedByUsername = User.FindFirst(ClaimTypes.Email)?.Value;

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            recipe.IsUserCreated = userRole switch
            {
                "Admin" or "Moderator" => false,
                _ => true
            };

            await _recipeRepository.CreateAsync(recipe, ct);
            var dto = _mapper.Map<RecipeDTO>(recipe);

            _logger.LogInformation("Recipe created with id: {Id}", recipe.Id);
            return CreatedAtRoute("GetRecipe", new { id = recipe.Id }, APIResponse<RecipeDTO>.Success(dto, HttpStatusCode.Created));
        }

        /// <summary>
        /// Create multiple recipes in a single request without images.
        /// </summary>
        [HttpPost("batch")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse<object>>> CreateRecipeBatchAsync(
            [FromBody] List<RecipeCreateDTO> createDTOs,
            CancellationToken ct = default)
        {
            if (createDTOs is null || createDTOs.Count == 0)
                return APIResponse<object>.Fail("Payload must contain at least one item", HttpStatusCode.BadRequest);

            _logger.LogInformation("Creating recipe batch: {Count} items", createDTOs.Count);

            foreach (var dto in createDTOs)
            {
                var recipe = _mapper.Map<Recipe>(dto);
                await _recipeRepository.CreateAsync(recipe, ct);
            }

            _logger.LogInformation("Recipe batch created successfully");
            return APIResponse<object>.Success(null, HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Delete recipe by id (owner or privileged roles).
        /// </summary>
        [HttpDelete("{id:guid}", Name = "DeleteRecipe")]
        [Authorize]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse<object>>> DeleteRecipeAsync([FromRoute] Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
                return APIResponse<object>.Fail("Invalid ID", HttpStatusCode.BadRequest);

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("DeleteRecipe rejected: no user id in claims");
                return APIResponse<object>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
            }

            var recipe = await _recipeRepository.GetAsync(u => u.Id == id, cancellationToken: ct);
            if (recipe is null)
            {
                _logger.LogWarning("DeleteRecipe: recipe not found {Id}", id);
                return APIResponse<object>.Fail("Recipe not found", HttpStatusCode.NotFound);
            }

            // Authorization: owner or privileged roles
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var isOwner = recipe.CreatedBy == userId;
            var isPrivileged = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!isOwner && !isPrivileged)
            {
                _logger.LogWarning("DeleteRecipe forbidden. User {UserId} is not owner of {RecipeId}", userId, id);
                return APIResponse<object>.Fail("You are not authorized to delete this recipe", HttpStatusCode.Forbidden);
            }

            if (!string.IsNullOrEmpty(recipe.ImageUrl))
            {
                try
                {
                    var fileName = Path.GetFileName(recipe.ImageUrl);
                    _logger.LogInformation("Deleting image from storage: {File}", fileName);
                    await _minIOService.DeleteImageAsync(fileName, ct);
                }
                catch (Exception imgEx)
                {
                    // Log but continue with recipe deletion to avoid orphan data inconsistencies
                    _logger.LogError(imgEx, "Failed to delete image for recipe {Id}", id);
                }
            }

            await _recipeRepository.RemoveAsync(recipe, ct);
            _logger.LogInformation("Recipe deleted: {Id}", id);

            return APIResponse<object>.Success(null, HttpStatusCode.NoContent);
        }
        /// <summary>
        /// Update a recipe completely (PUT).
        /// </summary>
        [HttpPut("{id:guid}", Name = "UpdateRecipe")]
        [Authorize]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType(typeof(APIResponse<object>))]
        public async Task<ActionResult<APIResponse<object>>> UpdateRecipeAsync(
            Guid id,
            [FromForm] RecipeUpdateDTO updateDTO,
            IFormFile? image,
            CancellationToken ct)
        {
            if (id != updateDTO.Id)
                return APIResponse<object>.Fail("Id in URL must match Id in DTO", HttpStatusCode.BadRequest);

            _logger.LogInformation("Validating update request for recipe {Id}", id);
            var validationResult = await _updateValidator.ValidateAsync((updateDTO, image), ct);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for recipe {Id}: {Errors}", id, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return APIResponse<object>.Fail(validationResult.Errors.Select(e => e.ErrorMessage).ToList(), HttpStatusCode.BadRequest);
            }

            var existingRecipe = await _recipeRepository.GetAsync(r => r.Id == id, cancellationToken: ct, isTracked: true);
            if (existingRecipe is null)
            {
                _logger.LogWarning("Recipe {Id} not found for update", id);
                return APIResponse<object>.Fail("Recipe not found", HttpStatusCode.NotFound);
            }

            if (!TryGetUserId(out var userId) || existingRecipe.CreatedBy != userId)
            {
                _logger.LogWarning("Unauthorized update attempt for recipe {Id} by user {UserId}", id, userId);
                return APIResponse<object>.Fail("You are not authorized to update this recipe", HttpStatusCode.Forbidden);
            }

            if (image is not null && image.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingRecipe.ImageUrl))
                {
                    var oldFileName = Path.GetFileName(existingRecipe.ImageUrl);
                    await _minIOService.DeleteImageAsync(oldFileName, ct);
                    _logger.LogInformation("Deleted old image {FileName} for recipe {Id}", oldFileName, id);
                }

                updateDTO.ImageUrl = await _minIOService.UploadImageAsync(image, ct);
                _logger.LogInformation("Uploaded new image for recipe {Id}", id);
            }
            else
            {
                updateDTO.ImageUrl = existingRecipe.ImageUrl;
            }

            updateDTO.TransliteratedName =
                updateDTO.Title != existingRecipe.Title
                    ? Transliterator.Transliterate(updateDTO.Title)
                    : existingRecipe.TransliteratedName;

            _mapper.Map(updateDTO, existingRecipe);
            await _recipeRepository.UpdateAsync(existingRecipe, ct);

            _logger.LogInformation("Recipe {Id} successfully updated", id);
            return APIResponse<object>.Success(null, HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Partially update a recipe (PATCH).
        /// </summary>
        [HttpPatch("{id:guid}", Name = "UpdatePartialRecipe")]
        [Authorize]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse<object>>> UpdatePartialRecipeAsync(
            Guid id,
            [FromBody] JsonPatchDocument<RecipeUpdateDTO> patchDTO,
            CancellationToken ct)
        {
            if (patchDTO is null || id == Guid.Empty)
                return APIResponse<object>.Fail("Invalid request", HttpStatusCode.BadRequest);

            _logger.LogInformation("Fetching recipe {Id} for partial update", id);
            var recipe = await _recipeRepository.GetAsync(r => r.Id == id, cancellationToken: ct, isTracked: true);
            if (recipe is null)
            {
                _logger.LogWarning("Recipe {Id} not found for patch update", id);
                return APIResponse<object>.Fail("Recipe not found", HttpStatusCode.NotFound);
            }

            if (!TryGetUserId(out var userId) || recipe.CreatedBy != userId)
            {
                _logger.LogWarning("Unauthorized patch attempt for recipe {Id} by user {UserId}", id, userId);
                return APIResponse<object>.Fail("You are not authorized to update this recipe", HttpStatusCode.Forbidden);
            }

            var recipeDTO = _mapper.Map<RecipeUpdateDTO>(recipe);
            patchDTO.ApplyTo(recipeDTO, ModelState);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Patch validation failed for recipe {Id}: {Errors}", id, string.Join(", ", errors));
                return APIResponse<object>.Fail(errors, HttpStatusCode.BadRequest);
            }

            var validationResult = await _updateValidator.ValidateAsync((recipeDTO, null), ct);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validator failed for recipe {Id}: {Errors}", id, string.Join(", ", errors));
                return APIResponse<object>.Fail(errors, HttpStatusCode.BadRequest);
            }

            _mapper.Map(recipeDTO, recipe);
            await _recipeRepository.UpdateAsync(recipe, ct);

            _logger.LogInformation("Recipe {Id} successfully patched", id);
            return APIResponse<object>.Success(null, HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Increment the view count of a recipe.
        /// </summary>
        [HttpPatch("{id:guid}/increment-views")]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse<object>>> IncrementViewCountAsync(Guid id, CancellationToken ct)
        {
            var success = await _recipeRepository.IncrementViewCountAsync(id, ct);

            if (!success)
            {
                _logger.LogWarning("Recipe {Id} not found while incrementing views", id);
                return APIResponse<object>.Fail($"Recipe with id {id} not found", HttpStatusCode.NotFound);
            }

            _logger.LogInformation("Incremented view count for recipe {Id}", id);
            return APIResponse<object>.Success("View count incremented successfully");
        }

        /// <summary>
        /// Filter recipes by various criteria with pagination and sorting.
        /// </summary>
        [HttpGet("filter")]
        [ProducesResponseType(typeof(APIResponse<PagedResponse<RecipeDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse<PagedResponse<RecipeDTO>>>> FilterRecipesAsync(
            [FromQuery] string filters,
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] string? sortOrder,
            CancellationToken ct)
        {
            _logger.LogInformation("Starting FilterRecipesAsync with filters: {Filters}, Page: {Page}, PageSize: {PageSize}, SortOrder: {SortOrder}",
                    filters, paginationParams.PageNumber, paginationParams.PageSize, sortOrder);

            var decodedFilters = Uri.UnescapeDataString(filters);
            var filterDto = ParseFilters(decodedFilters);

            _logger.LogDebug("Parsed filter DTO: {@FilterDto}", filterDto);

            var list = await _recipeRepository.FilterRecipesAsync(filterDto, paginationParams, sortOrder, ct);
            var recipeDtos = _mapper.Map<List<RecipeDTO>>(list);

            _logger.LogInformation("Retrieved {Count} recipes after filtering", recipeDtos.Count);

            var filterExpression = _recipeRepository.GetFilterExpression(filterDto);
            var totalCount = await _recipeRepository.CountAsync(filterExpression, ct);

            _logger.LogInformation("Total matching recipes count: {TotalCount}", totalCount);

            var pagedResponse = new PagedResponse<RecipeDTO>(
                recipeDtos, totalCount, paginationParams.PageNumber, paginationParams.PageSize);

            return APIResponse<PagedResponse<RecipeDTO>>.Success(pagedResponse);
        }

        /// <summary>
        /// Get distinct filter options (checkboxes) for a category.
        /// </summary>
        [HttpGet("checkboxfilter")]
        [ProducesResponseType(typeof(APIResponse<CheckboxFilter>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse<CheckboxFilter>>> CheckboxFilterAsync(
            [FromQuery] Guid categoryId, 
            CancellationToken ct)
        {
            _logger.LogInformation("Fetching checkbox filters for category {CategoryId}", categoryId);

            var list = await _recipeRepository.GetAllAsync(r => r.CategoryId == categoryId, cancellationToken: ct);
            _logger.LogDebug("Fetched {Count} recipes for checkbox filter computation", list.Count);

            var recipes = _mapper.Map<List<RecipeDTO>>(list);

            var filter = new CheckboxFilter
            {
                Diets = recipes.Where(r => r.Diets != null).SelectMany(r => r.Diets).Distinct().OrderBy(d => d).ToList(),
                Allergens = recipes.Where(r => r.Allergens != null).SelectMany(r => r.Allergens).Distinct().OrderBy(a => a).ToList(),
                Cuisines = recipes.Where(r => !string.IsNullOrEmpty(r.Cuisine)).Select(r => r.Cuisine).Distinct().OrderBy(c => c).ToList(),
                Tags = recipes.Where(r => r.Tags != null).SelectMany(r => r.Tags).Distinct().OrderBy(t => t).ToList()
            };

            _logger.LogInformation("Checkbox filter computed: {@Filter}", filter);

            return APIResponse<CheckboxFilter>.Success(filter);
        }

        /// <summary>
        /// Get all recipes for a specific category.
        /// </summary>
        [HttpGet("by-category/{categoryId:guid}")]
        [ProducesResponseType(typeof(APIResponse<List<RecipeDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(APIResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse<List<RecipeDTO>>>> GetRecipesWithSimilarCategoryAsync(
            Guid categoryId, 
            CancellationToken ct)
        {
            _logger.LogInformation("Fetching recipes for category {CategoryId}", categoryId);

            var list = await _recipeRepository.GetAllAsync(r => r.CategoryId == categoryId, cancellationToken: ct);
            var recipes = _mapper.Map<List<RecipeDTO>>(list);

            _logger.LogInformation("Fetched {Count} recipes for category {CategoryId}", recipes.Count, categoryId);

            return APIResponse<List<RecipeDTO>>.Success(recipes);
        }

        
    }
}
