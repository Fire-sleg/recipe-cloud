using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeService.Data;
using RecipeService.Models;
using RecipeService.Models.Collections;
using RecipeService.Models.Collections.DTOs;
using RecipeService.Models.Pagination;
using RecipeService.Models.Recipes;
using RecipeService.Repository;
using System.Net;
using System.Security.Claims;

namespace RecipeService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollectionController : ControllerBase
    {
        protected APIResponse _response = new APIResponse();
        private readonly ICollectionRepository _dbCollection;
        private readonly IRecipeRepository _dbRecipe;
        private readonly IMapper _mapper;
        private readonly IValidator<CollectionCreateDTO> _createValidator;
        private readonly IValidator<CollectionUpdateDTO> _updateValidator;

        public CollectionController(
            ICollectionRepository dbCollection,
            IRecipeRepository dbRecipe,
            IMapper mapper,
            IValidator<CollectionCreateDTO> createValidator,
            IValidator<CollectionUpdateDTO> updateValidator)
        {
            _dbCollection = dbCollection;
            _dbRecipe = dbRecipe;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCollectionsAsync()
        {
            try
            {
                var collections = await _dbCollection.GetAllAsync(null);
                var collectionDTOs = _mapper.Map<List<CollectionDTO>>(collections);

                _response.Result = collectionDTOs;
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorsMessages.Add(ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpGet("{id:guid}", Name = "GetCollection")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest(_response);
                }

                var collection = await _dbCollection.GetAsync(c => c.Id == id);
                if (collection == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }

                _response.Result = _mapper.Map<CollectionDTO>(collection);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorsMessages.Add(ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> CreateCollectionAsync([FromBody] CollectionCreateDTO createDTO)
        {
            try
            {
                var validationResult = await _createValidator.ValidateAsync(createDTO);
                if (!validationResult.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return BadRequest(_response);
                }

                // Перевірка існування рецептів
                if (createDTO.RecipeIds.Any())
                {
                    var recipes = await _dbRecipe.GetAllAsync(r => createDTO.RecipeIds.Contains(r.Id));
                    if (recipes.Count != createDTO.RecipeIds.Count)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorsMessages.Add("One or more recipe IDs are invalid");
                        return BadRequest(_response);
                    }
                }

                var collection = _mapper.Map<Collection>(createDTO);
                collection.CreatedBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                if (createDTO.RecipeIds.Any())
                {
                    var recipes = await _dbRecipe.GetAllAsync(r => createDTO.RecipeIds.Contains(r.Id));
                    collection.Recipes = recipes;
                }
                else
                {
                    collection.Recipes = new List<Recipe>();
                }

                UpdateNutritionalValues(collection);

                await _dbCollection.CreateAsync(collection);

                _response.Result = _mapper.Map<CollectionDTO>(collection);
                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccess = true;

                return CreatedAtRoute("GetCollection", new { id = collection.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorsMessages.Add(ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPut("{id:guid}", Name = "UpdateCollection")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<APIResponse>> UpdateCollectionAsync(Guid id, [FromBody] CollectionUpdateDTO updateDTO)
        {
            try
            {
                if (id != updateDTO.Id)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages.Add("Id in URL must match Id in DTO.");
                    return BadRequest(_response);
                }

                var validationResult = await _updateValidator.ValidateAsync(updateDTO);
                if (!validationResult.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return BadRequest(_response);
                }

                if (updateDTO.RecipeIds.Any())
                {
                    var recipes = await _dbRecipe.GetAllAsync(r => updateDTO.RecipeIds.Contains(r.Id));
                    if (recipes.Count != updateDTO.RecipeIds.Count)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        _response.ErrorsMessages.Add("One or more recipe IDs are invalid");
                        return BadRequest(_response);
                    }
                }

                var existingCollection = await _dbCollection.GetAsync(c => c.Id == id);
                if (existingCollection == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }

                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (existingCollection.CreatedBy != userId)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages.Add("You are not authorized to update this collection");
                    return StatusCode(StatusCodes.Status403Forbidden, _response);
                }

                var collection = _mapper.Map<Collection>(updateDTO);
                collection.CreatedBy = existingCollection.CreatedBy;
                collection.CreatedAt = existingCollection.CreatedAt;

                if (updateDTO.RecipeIds.Any())
                {
                    var recipes = await _dbRecipe.GetAllAsync(r => updateDTO.RecipeIds.Contains(r.Id));
                    collection.Recipes = recipes;
                }
                else
                {
                    collection.Recipes = new List<Recipe>();
                }

                UpdateNutritionalValues(collection);

                await _dbCollection.UpdateAsync(collection);

                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorsMessages.Add(ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpDelete("{id:guid}", Name = "DeleteCollection")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<APIResponse>> DeleteCollectionAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest(_response);
                }

                var collection = await _dbCollection.GetAsync(c => c.Id == id);
                if (collection == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }

                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (collection.CreatedBy != userId)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages.Add("You are not authorized to delete this collection");
                    return StatusCode(StatusCodes.Status403Forbidden, _response);
                }

                await _dbCollection.RemoveAsync(collection);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorsMessages.Add(ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        private void UpdateNutritionalValues(Collection collection)
        {
            collection.TotalCalories = collection.Recipes.Sum(r => r.Calories);
            collection.TotalProtein = collection.Recipes.Sum(r => r.Protein);
            collection.TotalFat = collection.Recipes.Sum(r => r.Fat);
            collection.TotalCarbohydrates = collection.Recipes.Sum(r => r.Carbohydrates);
        }
    }
}