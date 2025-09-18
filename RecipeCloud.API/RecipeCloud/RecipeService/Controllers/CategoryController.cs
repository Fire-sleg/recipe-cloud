using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RecipeService.Models;
using RecipeService.Models.Categories;
using RecipeService.Models.Categories.DTOs;
using RecipeService.Repository;
using RecipeService.Services;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RecipeService.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly IRedisCache _redis;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            ICategoryRepository categoryRepository,
            IMapper mapper,
            IRedisCache redis,
            ILogger<CategoryController> logger)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _redis = redis;
            _logger = logger;
        }

        /// <summary>
        /// Get all categories
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<APIResponse<List<CategoryDTO>>>> GetCategoriesAsync()
        {
            var cacheKey = "categories:all";
            _logger.LogInformation("Fetching all categories with cache key: {CacheKey}", cacheKey);

            var cached = await _redis.GetAsync<List<CategoryDTO>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Categories retrieved from cache.");
                return APIResponse<List<CategoryDTO>>.Success(cached);
            }

            var categories = await _categoryRepository.GetAllAsync();
            var categoryDtos = _mapper.Map<List<CategoryDTO>>(categories);

            await _redis.SetAsync(cacheKey, categoryDtos, TimeSpan.FromHours(24));
            _logger.LogInformation("Categories cached successfully.");

            return APIResponse<List<CategoryDTO>>.Success(categoryDtos);
        }

        /// <summary>
        /// Get base categories (without parent)
        /// </summary>
        [HttpGet("base")]
        public async Task<ActionResult<APIResponse<List<CategoryDTO>>>> GetBaseCategoriesAsync()
        {
            var cacheKey = "categories:base";
            _logger.LogInformation("Fetching base categories with cache key: {CacheKey}", cacheKey);

            var cached = await _redis.GetAsync<List<CategoryDTO>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Base categories retrieved from cache.");
                return APIResponse<List<CategoryDTO>>.Success(cached);
            }

            var categories = await _categoryRepository.GetAllAsync(
                c => c.ParentCategoryId == null || c.ParentCategoryId == Guid.Empty,
                withSubCategories: true,
                withRecipes: false
            );

            categories = categories.OrderBy(c => c.Order).ToList();
            var categoryDtos = _mapper.Map<List<CategoryDTO>>(categories);

            await _redis.SetAsync(cacheKey, categoryDtos, TimeSpan.FromHours(24));
            _logger.LogInformation("Base categories cached successfully.");

            return APIResponse<List<CategoryDTO>>.Success(categoryDtos);
        }

        /// <summary>
        /// Get sub-categories
        /// </summary>
        [HttpGet("sub")]
        public async Task<ActionResult<APIResponse<List<CategoryDTO>>>> GetSubCategoriesAsync()
        {
            var cacheKey = "categories:sub";
            _logger.LogInformation("Fetching sub-categories with cache key: {CacheKey}", cacheKey);

            var cached = await _redis.GetAsync<List<CategoryDTO>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Sub-categories retrieved from cache.");
                return APIResponse<List<CategoryDTO>>.Success(cached);
            }

            var categories = await _categoryRepository.GetAllAsync(
                c => c.ParentCategoryId != null && c.ParentCategoryId != Guid.Empty,
                withSubCategories: true,
                withRecipes: false
            );

            categories = categories.OrderBy(c => c.Order).ToList();
            var categoryDtos = _mapper.Map<List<CategoryDTO>>(categories);

            await _redis.SetAsync(cacheKey, categoryDtos, TimeSpan.FromHours(24));
            _logger.LogInformation("Sub-categories cached successfully.");

            return APIResponse<List<CategoryDTO>>.Success(categoryDtos);
        }

        
        /// <summary>
        /// Get category by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<APIResponse<CategoryDTO>>> GetAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("Invalid category ID: {Id}", id);
                return APIResponse<CategoryDTO>.Fail("Invalid ID", HttpStatusCode.BadRequest);
            }

            var cacheKey = $"categories:{id}";
            var cached = await _redis.GetAsync<CategoryDTO>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Category {Id} retrieved from cache.", id);
                return APIResponse<CategoryDTO>.Success(cached);
            }

            var category = await _categoryRepository.GetAsync(c => c.Id == id);
            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found.", id);
                return APIResponse<CategoryDTO>.Fail("Category not found", HttpStatusCode.NotFound);
            }

            var dto = _mapper.Map<CategoryDTO>(category);
            await _redis.SetAsync(cacheKey, dto, TimeSpan.FromHours(24));
            _logger.LogInformation("Category {Id} cached successfully.", id);

            return APIResponse<CategoryDTO>.Success(dto);
        }

        /// <summary>
        /// Get category by transliterated name
        /// </summary>
        [HttpGet("{transliteratedName}")]
        public async Task<ActionResult<APIResponse<CategoryDTO>>> GetByTransliteratedNameAsync(string transliteratedName)
        {
            if (string.IsNullOrWhiteSpace(transliteratedName))
            {
                _logger.LogWarning("Invalid transliterated name parameter.");
                return APIResponse<CategoryDTO>.Fail("Invalid transliterated name", HttpStatusCode.BadRequest);
            }

            var cacheKey = $"categories:translit:{transliteratedName.ToLower()}";
            var cached = await _redis.GetAsync<CategoryDTO>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Category {TransliteratedName} retrieved from cache.", transliteratedName);
                return APIResponse<CategoryDTO>.Success(cached);
            }

            var category = await _categoryRepository.GetAsync(c => c.TransliteratedName == transliteratedName);
            if (category == null)
            {
                _logger.LogWarning("Category with transliterated name {TransliteratedName} not found.", transliteratedName);
                return APIResponse<CategoryDTO>.Fail("Category not found", HttpStatusCode.NotFound);
            }

            var dto = _mapper.Map<CategoryDTO>(category);
            await _redis.SetAsync(cacheKey, dto, TimeSpan.FromHours(24));
            _logger.LogInformation("Category {TransliteratedName} cached successfully.", transliteratedName);

            return APIResponse<CategoryDTO>.Success(dto);
        }


        /// <summary>
        /// Create category
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<APIResponse<object>>> CreateAsync([FromBody] CategoryCreateDTO createDTO)
        {
            if (createDTO == null || !ModelState.IsValid)
            {
                _logger.LogWarning("Invalid category creation attempt.");
                return APIResponse<object>.Fail("Invalid input", HttpStatusCode.BadRequest);
            }

            var exists = await _categoryRepository.GetAsync(c => c.Name.ToLower() == createDTO.Name.ToLower());
            if (exists != null)
            {
                _logger.LogWarning("Duplicate category name: {Name}", createDTO.Name);
                return APIResponse<object>.Fail("Category already exists", HttpStatusCode.BadRequest);
            }

            var model = _mapper.Map<Category>(createDTO);
            await _categoryRepository.CreateAsync(model);

            _logger.LogInformation("Category created successfully: {Name}", createDTO.Name);
            return APIResponse<object>.Success(null);
        }

        /// <summary>
        /// Create multiple categories in batch
        /// </summary>
        [HttpPost("batch")]
        public async Task<ActionResult<APIResponse<object>>> CreateBatchAsync([FromBody] List<CategoryCreateDTO> createDTOs)
        {
            if (createDTOs == null || !createDTOs.Any() || !ModelState.IsValid)
            {
                _logger.LogWarning("Invalid batch category creation attempt.");
                return APIResponse<object>.Fail("Invalid input", HttpStatusCode.BadRequest);
            }

            int createdCount = 0;
            foreach (var dto in createDTOs)
            {
                var exists = await _categoryRepository.GetAsync(
                    u => u.Name.ToLower() == dto.Name.ToLower(),
                    withSubCategories: false,
                    withRecipes: false,
                    tracked: false);

                if (exists != null)
                {
                    _logger.LogWarning("Skipping duplicate category name: {Name}.", dto.Name);
                    continue;
                }

                var model = _mapper.Map<Category>(dto);
                await _categoryRepository.CreateAsync(model);
                createdCount++;
            }

            _logger.LogInformation("{Count} categories created successfully.", createdCount);
            return APIResponse<object>.Success(null);
        }

        /// <summary>
        /// Delete category
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<APIResponse<object>>> DeleteAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("Invalid category ID: {Id}", id);
                return APIResponse<object>.Fail("Invalid ID", HttpStatusCode.BadRequest);
            }

            var category = await _categoryRepository.GetAsync(c => c.Id == id);
            if (category == null)
            {
                _logger.LogWarning("Category {Id} not found.", id);
                return APIResponse<object>.Fail("Category not found", HttpStatusCode.NotFound);
            }

            await _categoryRepository.RemoveAsync(category);
            _logger.LogInformation("Category deleted successfully: {Id}", id);

            return APIResponse<object>.Success(null);
        }

        /// <summary>
        /// Update category
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<APIResponse<object>>> UpdateAsync(Guid id, [FromBody] CategoryUpdateDTO updateDTO)
        {
            if (updateDTO == null || id != updateDTO.Id || !ModelState.IsValid)
            {
                _logger.LogWarning("Invalid category update attempt: {Id}", id);
                return APIResponse<object>.Fail("Invalid input", HttpStatusCode.BadRequest);
            }

            var model = _mapper.Map<Category>(updateDTO);
            await _categoryRepository.UpdateAsync(model);

            _logger.LogInformation("Category updated successfully: {Id}", id);
            return APIResponse<object>.Success(null);
        }

        /// <summary>
        /// Partially update a category using JSON Patch
        /// </summary>
        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<APIResponse<object>>> UpdatePartialCategoryAsync(
            Guid id,
            [FromBody] JsonPatchDocument<CategoryUpdateDTO> patchDTO)
        {
            if (patchDTO == null)
            {
                _logger.LogWarning("Invalid patch document for category ID: {Id}.", id);
                return APIResponse<object>.Fail("Invalid patch document", HttpStatusCode.BadRequest);
            }

            var category = await _categoryRepository.GetAsync(u => u.Id == id, tracked: false);
            if (category == null)
            {
                _logger.LogWarning("Category with ID {Id} not found for partial update.", id);
                return APIResponse<object>.Fail("Category not found", HttpStatusCode.NotFound);
            }

            var categoryDTO = _mapper.Map<CategoryUpdateDTO>(category);
            patchDTO.ApplyTo(categoryDTO, ModelState);

            if (!TryValidateModel(categoryDTO))
            {
                _logger.LogWarning("Model state invalid after applying patch for category ID: {Id}.", id);
                return APIResponse<object>.Fail("Invalid data after patch", HttpStatusCode.BadRequest);
            }

            var model = _mapper.Map<Category>(categoryDTO);
            await _categoryRepository.UpdateAsync(model);

            _logger.LogInformation("Category with ID {Id} partially updated.", id);
            return APIResponse<object>.Success(null);
        }
    }
}