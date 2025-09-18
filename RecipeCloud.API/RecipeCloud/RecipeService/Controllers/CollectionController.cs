using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;
using RecipeService.Models;
using RecipeService.Models.Collections;
using RecipeService.Models.Collections.DTOs;
using RecipeService.Models.Recipes;
using RecipeService.Repository;
using RecipeService.Services;
using System.Net;
using System.Security.Claims;

namespace RecipeService.Controllers
{
    [Route("api/collections")]
    [ApiController]
    public class CollectionController : ControllerBase
    {
        private readonly IValidator<CollectionCreateDTO> _createValidator;
        private readonly IValidator<CollectionUpdateDTO> _updateValidator;
        private readonly ICollectionRepository _collectionRepository;
        private readonly IRecipeRepository _recipeRepository;
        private readonly IRedisCache _redis;
        private readonly IMapper _mapper;
        private readonly ILogger<CollectionController> _logger;

        public CollectionController(
            IValidator<CollectionCreateDTO> createValidator,
            IValidator<CollectionUpdateDTO> updateValidator,
            ICollectionRepository collectionRepository,
            IRecipeRepository recipeRepository,
            IRedisCache redis,
            IMapper mapper,
            ILogger<CollectionController> logger)
        {
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _collectionRepository = collectionRepository;
            _recipeRepository = recipeRepository;
            _redis = redis;
            _mapper = mapper;
            _logger = logger;
        }

        #region Helpers
        private bool TryGetUserId(out Guid userId)
        {
            var userIdClaim = User.FindFirst("ident")?.Value;
            return Guid.TryParse(userIdClaim, out userId);
        }

        private bool IsUserAdminOrModerator()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            return userRole switch
            {
                "Admin" or "Moderator" => false,
                _ => true
            };
        }
        #endregion

        /// <summary>
        /// Get all collections
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<APIResponse<List<CollectionDTO>>>> GetCollectionsAsync(CancellationToken ct)
        {
            var cacheKey = "collections:all";
            _logger.LogInformation("Fetching all collections with cache key: {CacheKey}", cacheKey);

            var cached = await _redis.GetAsync<List<CollectionDTO>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Collections retrieved from cache.");
                return APIResponse<List<CollectionDTO>>.Success(cached);
            }

            var collections = await _collectionRepository.GetAllAsync(cancellationToken: ct);
            var dtos = _mapper.Map<List<CollectionDTO>>(collections);

            await _redis.SetAsync(cacheKey, dtos, TimeSpan.FromHours(24));
            _logger.LogInformation("Collections cached successfully.");

            return APIResponse<List<CollectionDTO>>.Success(dtos);
        }

        /// <summary>
        /// Get collection by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<APIResponse<CollectionDTO>>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("Invalid collection ID: {Id}", id);
                return APIResponse<CollectionDTO>.Fail("Invalid ID", HttpStatusCode.BadRequest);
            }

            var cacheKey = $"collections:{id}";
            var cached = await _redis.GetAsync<CollectionDTO>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Collection {Id} retrieved from cache.", id);
                return APIResponse<CollectionDTO>.Success(cached);
            }

            var collection = await _collectionRepository.GetAsync(c => c.Id == id, cancellationToken: ct);
            if (collection == null)
            {
                _logger.LogWarning("Collection {Id} not found.", id);
                return APIResponse<CollectionDTO>.Fail("Collection not found", HttpStatusCode.NotFound);
            }

            var dto = _mapper.Map<CollectionDTO>(collection);
            await _redis.SetAsync(cacheKey, dto, TimeSpan.FromHours(24));
            _logger.LogInformation("Collection {Id} cached successfully.", id);

            return APIResponse<CollectionDTO>.Success(dto);
        }

        /// <summary>
        /// Create collection
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<APIResponse<object>>> CreateAsync(
            [FromForm] CollectionCreateDTO createDTO,
            CancellationToken ct)
        {
            var validationResult = await _createValidator.ValidateAsync(createDTO, ct);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for collection creation.");
                return APIResponse<object>.Fail(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)), HttpStatusCode.BadRequest);
            }
            
            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized attempt to create collection.");
                return APIResponse<object>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
            }

            if (createDTO.RecipeIds.Any())
            {
                var recipes = await _recipeRepository.GetAllAsync(r => createDTO.RecipeIds.Contains(r.Id));
                if (recipes.Count != createDTO.RecipeIds.Count)
                {
                    _logger.LogWarning("Invalid recipe IDs in collection creation.");
                    return APIResponse<object>.Fail("One or more recipe IDs are invalid.", HttpStatusCode.BadRequest);
                }
            }

            var collection = _mapper.Map<Collection>(createDTO);
            collection.CreatedBy = userId;
            collection.Recipes = createDTO.RecipeIds.Any()
                ? await _recipeRepository.GetAllAsync(r => createDTO.RecipeIds.Contains(r.Id))
                : new List<Recipe>();

            await _collectionRepository.CreateAsync(collection, cancellationToken: ct);
            _logger.LogInformation("Collection created successfully by user {UserId}", userId);

            return APIResponse<object>.Success(_mapper.Map<CollectionDTO>(collection), HttpStatusCode.Created);
        }

        /// <summary>
        /// Update collection
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<APIResponse<object>>> UpdateAsync(Guid id, [FromForm] CollectionUpdateDTO updateDTO, CancellationToken ct)
        {
            if (id != updateDTO.Id)
            {
                _logger.LogWarning("Mismatched IDs in update request: {Id} vs {DtoId}", id, updateDTO.Id);
                return APIResponse<object>.Fail("ID mismatch", HttpStatusCode.BadRequest);
            }

            var validationResult = await _updateValidator.ValidateAsync(updateDTO, ct);
            if (!validationResult.IsValid)
            {
                return APIResponse<object>.Fail(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)), HttpStatusCode.BadRequest);
            }

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized attempt to update collection.");
                return APIResponse<object>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
            }

            var existingCollection = await _collectionRepository.GetAsync(c => c.Id == id, isTracked: true, cancellationToken: ct);
            if (existingCollection == null)
            {
                return APIResponse<object>.Fail("Collection not found", HttpStatusCode.NotFound);
            }

            if (existingCollection.CreatedBy != userId || !IsUserAdminOrModerator())
            {
                return APIResponse<object>.Fail("Forbidden", HttpStatusCode.Forbidden);
            }

            List<Recipe> recipes = new List<Recipe>();
            if (updateDTO.RecipeIds.Any())
            {
                recipes = await _recipeRepository.GetAllAsync(r => updateDTO.RecipeIds.Contains(r.Id));
                if (recipes.Count != updateDTO.RecipeIds.Count)
                {
                    _logger.LogWarning("Invalid recipe IDs in collection update ID: {Id}.", id);
                    return APIResponse<object>.Fail("One or more recipe IDs are invalid", HttpStatusCode.BadRequest);
                }
            }

            // Update recipes
            var newRecipeIds = new HashSet<Guid>(updateDTO.RecipeIds);
            existingCollection.Recipes.RemoveAll(r => !newRecipeIds.Contains(r.Id));

            var existingRecipeIds = existingCollection.Recipes.Select(r => r.Id).ToHashSet();
            foreach (var recipe in recipes.Where(r => !existingRecipeIds.Contains(r.Id)))
            {
                existingCollection.Recipes.Add(recipe);
            }

            UpdateNutritionalValues(existingCollection);

            await _collectionRepository.UpdateAsync(existingCollection, cancellationToken: ct);
            _logger.LogInformation("Collection {Id} updated successfully by {UserId}", id, userId);

            return APIResponse<object>.Success(null, HttpStatusCode.NoContent);
        }

        /*
         
        var userIdClaim = User.FindFirst("ident")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Unauthorized attempt to update collection ID: {Id}.", id);
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("User ID not found or invalid.");
                    return Unauthorized(response);
                }

                _logger.LogInformation("Fetching collection for update with ID: {Id} by user: {UserId}.", id, userId);
                var existingCollection = await _collectionRepository.GetAsync(c => c.Id == id, isTracked: true, cancellationToken: ct);
                if (existingCollection == null)
                {
                    _logger.LogWarning("Collection with ID {Id} not found for update.", id);
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccess = false;
                    return NotFound(response);
                }

                if (existingCollection.CreatedBy != userId)
                {
                    _logger.LogWarning("Forbidden update attempt on collection ID: {Id} by user: {UserId}.", id, userId);
                    response.StatusCode = HttpStatusCode.Forbidden;
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("You are not authorized to update this collection.");
                    return StatusCode(StatusCodes.Status403Forbidden, response);
                }

                List<Recipe> recipes = new List<Recipe>();
                if (updateDTO.RecipeIds.Any())
                {
                    recipes = await _recipeRepository.GetAllAsync(r => updateDTO.RecipeIds.Contains(r.Id));
                    if (recipes.Count != updateDTO.RecipeIds.Count)
                    {
                        _logger.LogWarning("Invalid recipe IDs in collection update ID: {Id}.", id);
                        response.StatusCode = HttpStatusCode.BadRequest;
                        response.IsSuccess = false;
                        response.ErrorMessages.Add("One or more recipe IDs are invalid.");
                        return BadRequest(response);
                    }
                }

                _mapper.Map(updateDTO, existingCollection);

                // Update recipes
                var newRecipeIds = new HashSet<Guid>(updateDTO.RecipeIds);
                existingCollection.Recipes.RemoveAll(r => !newRecipeIds.Contains(r.Id));

                var existingRecipeIds = existingCollection.Recipes.Select(r => r.Id).ToHashSet();
                foreach (var recipe in recipes.Where(r => !existingRecipeIds.Contains(r.Id)))
                {
                    existingCollection.Recipes.Add(recipe);
                }

                UpdateNutritionalValues(existingCollection);

                _logger.LogInformation("Updating collection with ID: {Id}.", id);
                await _collectionRepository.UpdateAsync(existingCollection, cancellationToken: ct);

                response.StatusCode = HttpStatusCode.NoContent;
                response.IsSuccess = true;
                return Ok(response);
         
         */

        /// <summary>
        /// Delete collection
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<APIResponse<object>>> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (id == Guid.Empty)
            {
                return APIResponse<object>.Fail("Invalid ID", HttpStatusCode.BadRequest);
            }

            
            if (!TryGetUserId(out var userId))
            {
                return APIResponse<object>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
            }

            var collection = await _collectionRepository.GetAsync(c => c.Id == id, cancellationToken: ct);
            if (collection == null)
            {
                return APIResponse<object>.Fail("Collection not found", HttpStatusCode.NotFound);
            }

            if (collection.CreatedBy != userId || !IsUserAdminOrModerator())
            {
                return APIResponse<object>.Fail("Forbidden", HttpStatusCode.Forbidden);
            }

            await _collectionRepository.RemoveAsync(collection, cancellationToken: ct);
            _logger.LogInformation("Collection {Id} deleted by user {UserId}", id, userId);

            return APIResponse<object>.Success(null, HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Get all collections by user
        /// </summary>
        [HttpGet("user/{userId:guid}")]
        public async Task<ActionResult<APIResponse<List<CollectionDTO>>>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var cacheKey = $"collections:user:{userId}";
            var cached = await _redis.GetAsync<List<CollectionDTO>>(cacheKey);
            if (cached != null)
            {
                return APIResponse<List<CollectionDTO>>.Success(cached);
            }

            var collections = await _collectionRepository.GetAllAsync(c => c.CreatedBy == userId, cancellationToken: ct);
            var dtos = _mapper.Map<List<CollectionDTO>>(collections);

            await _redis.SetAsync(cacheKey, dtos, TimeSpan.FromHours(24));
            return APIResponse<List<CollectionDTO>>.Success(dtos);
        }

        /// <summary>
        /// Adds a recipe to a collection.
        /// </summary>
        [HttpPost("{collectionId:guid}/recipes")]
        [Authorize]
        public async Task<ActionResult<APIResponse<CollectionDTO>>> AddRecipeToCollectionAsync(
            Guid collectionId,
            [FromBody] AddRecipeRequest request,
            CancellationToken ct)
        {
            if (collectionId == Guid.Empty || request.RecipeId == Guid.Empty)
            {
                _logger.LogWarning("Invalid IDs in add request: Collection {CollectionId}, Recipe {RecipeId}.", collectionId, request.RecipeId);
                return APIResponse<CollectionDTO>.Fail("CollectionId and RecipeId are required.", HttpStatusCode.BadRequest);
            }

            if (!TryGetUserId(out var userId))
            {
                return APIResponse<CollectionDTO>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
            }

            var collection = await _collectionRepository.GetAsync(c => c.Id == collectionId, isTracked: true, cancellationToken: ct);
            if (collection == null)
            {
                return APIResponse<CollectionDTO>.Fail("Collection not found", HttpStatusCode.NotFound);
            }

            if (collection.CreatedBy != userId || !IsUserAdminOrModerator())
            {
                return APIResponse<CollectionDTO>.Fail("You are not authorized to modify this collection.", HttpStatusCode.Forbidden);
            }

            var recipe = await _recipeRepository.GetAsync(r => r.Id == request.RecipeId, cancellationToken: ct);
            if (recipe == null)
            {
                return APIResponse<CollectionDTO>.Fail("Recipe not found.", HttpStatusCode.NotFound);
            }

            if (collection.Recipes.Any(r => r.Id == request.RecipeId))
            {
                return APIResponse<CollectionDTO>.Fail("Recipe already in collection.", HttpStatusCode.BadRequest);
            }

            collection.Recipes.Add(recipe);
            UpdateNutritionalValues(collection);
            await _collectionRepository.UpdateAsync(collection, cancellationToken: ct);

            var dto = _mapper.Map<CollectionDTO>(collection);
            return APIResponse<CollectionDTO>.Success(dto, HttpStatusCode.OK);
        }

        /// <summary>
        /// Removes a recipe from a collection.
        /// </summary>
        [HttpDelete("{collectionId:guid}/recipes/{recipeId:guid}")]
        [Authorize]
        public async Task<ActionResult<APIResponse<CollectionDTO>>> RemoveRecipeFromCollectionAsync(
            Guid collectionId,
            Guid recipeId,
            CancellationToken ct)
        {
            if (collectionId == Guid.Empty || recipeId == Guid.Empty)
            {
                return APIResponse<CollectionDTO>.Fail("CollectionId and RecipeId are required.", HttpStatusCode.BadRequest);
            }

            if (!TryGetUserId(out var userId))
            {
                return APIResponse<CollectionDTO>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
            }

            var collection = await _collectionRepository.GetAsync(c => c.Id == collectionId, isTracked: true, cancellationToken: ct);
            if (collection == null)
            {
                return APIResponse<CollectionDTO>.Fail("Collection not found", HttpStatusCode.NotFound);
            }

            if (collection.CreatedBy != userId || !IsUserAdminOrModerator())
            {
                return APIResponse<CollectionDTO>.Fail("You are not authorized to modify this collection.", HttpStatusCode.Forbidden);
            }

            var recipeToRemove = collection.Recipes.FirstOrDefault(r => r.Id == recipeId);
            if (recipeToRemove == null)
            {
                return APIResponse<CollectionDTO>.Fail("Recipe not found in collection.", HttpStatusCode.NotFound);
            }

            collection.Recipes.Remove(recipeToRemove);
            UpdateNutritionalValues(collection);
            await _collectionRepository.UpdateAsync(collection, cancellationToken: ct);

            var dto = _mapper.Map<CollectionDTO>(collection);
            return APIResponse<CollectionDTO>.Success(dto, HttpStatusCode.OK);
        }

        private void UpdateNutritionalValues(Collection collection)
        {
            collection.TotalCalories = collection.Recipes.Sum(r => r.Calories);
            collection.TotalProtein = collection.Recipes.Sum(r => r.Protein);
            collection.TotalFat = collection.Recipes.Sum(r => r.Fat);
            collection.TotalCarbohydrates = collection.Recipes.Sum(r => r.Carbohydrates);
        }
    }

    public class AddRecipeRequest
    {
        public Guid RecipeId { get; set; }
    }
}