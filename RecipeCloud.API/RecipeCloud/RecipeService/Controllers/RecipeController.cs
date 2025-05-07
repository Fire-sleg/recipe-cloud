using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using RecipeService.Data;
using RecipeService.Models;
using RecipeService.Models.DTOs;
using RecipeService.Models.Pagination;
using RecipeService.Repository;
using RecipeService.Services;
using System.Net;
using System.Security.Claims;

namespace RecipeService.Controllers
{
    // Controllers/RecipeController.cs
    [Route("api/[controller]")]
    [ApiController]
    public class RecipeController : ControllerBase
    {
        protected APIResponse _response = new APIResponse();
        private readonly IRecipeRepository _dbRecipe;
        private readonly IMapper _mapper;
        private readonly IMinIOService _minIOService;
        private readonly IValidator<(RecipeCreateDTO DTO, IFormFile Image)> _createValidator;
        private readonly IValidator<(RecipeUpdateDTO DTO, IFormFile Image)> _updateValidator;
        public RecipeController(
            IRecipeRepository dbRecipe, 
            IMapper mapper, 
            IMinIOService minIOService, 
            IValidator<(RecipeCreateDTO DTO, IFormFile Image)> createValidator,
            IValidator<(RecipeUpdateDTO DTO, IFormFile Image)> updateValidator)
        {
            _dbRecipe = dbRecipe;
            _mapper = mapper;
            _minIOService = minIOService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecipesAsync([FromQuery] PaginationParams paginationParams)
        {
            try
            {
                var list = await _dbRecipe.GetAllAsync(null, paginationParams);
                var recipes = _mapper.Map<List<RecipeDTO>>(list);

                var totalCount = await _dbRecipe.CountAsync();
                var pagedResponse = new PagedResponse<RecipeDTO>(recipes, totalCount, paginationParams.PageNumber, paginationParams.PageSize);



                _response.Result = pagedResponse;
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorsMessages.Add(ex.ToString());
            }
            return StatusCode(StatusCodes.Status500InternalServerError, _response);
        }

        [HttpGet("{id:Guid}", Name = "GetRecipe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetAsync(Guid id)
        {
            try
            {
                if (id == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest(_response);
                }
                var recipe = await _dbRecipe.GetAsync(u => u.Id == id);
                _response.Result = _mapper.Map<RecipeDTO>(recipe);
                if (recipe != null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorsMessages.Add(ex.ToString());
            }
            return StatusCode(StatusCodes.Status500InternalServerError, _response);

        }


        [HttpPost]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateRecipeAsync([FromForm] RecipeCreateDTO createDTO, IFormFile image)
        {
            try
            {
                // Валідація DTO та зображення
                var createValidationResult = await _createValidator.ValidateAsync((createDTO, image));
                if (!createValidationResult.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages = createValidationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return BadRequest(_response);
                }

                var imageUrl = await _minIOService.UploadImageAsync(image);
                var recipe = _mapper.Map<Recipe>(createDTO);
                recipe.ImageUrl = imageUrl;
                recipe.CreatedBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                await _dbRecipe.CreateAsync(recipe);

                _response.Result = _mapper.Map<RecipeDTO>(recipe);
                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccess = true;

                return CreatedAtRoute("GetRecipe", new { id = recipe.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorsMessages.Add(ex.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }


        [HttpDelete("{id:Guid}", Name = "DeleteRecipe")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> DeleteRecipeAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest(_response);
                }

                var recipe = await _dbRecipe.GetAsync(u => u.Id == id);
                if (recipe == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }

                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (recipe.CreatedBy != userId)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages.Add("You are not authorized to delete this recipe");
                    return StatusCode(StatusCodes.Status403Forbidden, _response);
                }

                if (!string.IsNullOrEmpty(recipe.ImageUrl))
                {
                    var fileName = Path.GetFileName(recipe.ImageUrl);
                    await _minIOService.DeleteImageAsync(fileName);
                }

                await _dbRecipe.RemoveAsync(recipe);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorsMessages = new List<string> { ex.ToString() };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPut("{id:Guid}", Name = "UpdateRecipe")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<APIResponse>> UpdateRecipeAsync(Guid id, [FromForm] RecipeUpdateDTO updateDTO, IFormFile image = null)
        {
            try
            {
                // Перевірка відповідності Id
                if (id != updateDTO.Id)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages.Add("Id in URL must match Id in DTO.");
                    return BadRequest(_response);
                }

                // Валідація DTO та зображення
                var updateValidationResult = await _updateValidator.ValidateAsync((updateDTO, image));
                if (!updateValidationResult.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages = updateValidationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return BadRequest(_response);
                }

                var existingRecipe = await _dbRecipe.GetAsync(u => u.Id == id);
                if (existingRecipe == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }

                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (existingRecipe.CreatedBy != userId)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages.Add("You are not authorized to update this recipe");
                    return StatusCode(StatusCodes.Status403Forbidden, _response);
                }

                if (image != null && image.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingRecipe.ImageUrl))
                    {
                        var oldFileName = Path.GetFileName(existingRecipe.ImageUrl);
                        await _minIOService.DeleteImageAsync(oldFileName);
                    }
                    updateDTO.ImageUrl = await _minIOService.UploadImageAsync(image);
                }

                var recipe = _mapper.Map<Recipe>(updateDTO);
                recipe.CreatedBy = existingRecipe.CreatedBy;
                recipe.CreatedAt = existingRecipe.CreatedAt;

                await _dbRecipe.UpdateAsync(recipe);

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

        [HttpPatch("{id:Guid}", Name = "UpdatePartialRecipe")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdatePartialRecipeAsync(Guid id, JsonPatchDocument<RecipeUpdateDTO> patchDTO)
        {
            try
            {
                if (patchDTO == null || id == Guid.Empty)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest(_response);
                }

                /*--AsNoTracking--*/
                var recipe = await _dbRecipe.GetAsync(u => u.Id == id, isTracked: false);
                if (recipe == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    return NotFound(_response);
                }

                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (recipe.CreatedBy != userId)
                {
                    _response.StatusCode = HttpStatusCode.Forbidden;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages.Add("You are not authorized to update this recipe");
                    return StatusCode(StatusCodes.Status403Forbidden, _response);
                }

                var recipeDTO = _mapper.Map<RecipeUpdateDTO>(recipe);
                patchDTO.ApplyTo(recipeDTO, ModelState);

                if (!ModelState.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(_response);
                }

                // Валідація патченого DTO
                var updateValidationResult = await _updateValidator.ValidateAsync((recipeDTO, null));
                if (!updateValidationResult.IsValid)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorsMessages = updateValidationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return BadRequest(_response);
                }

                var updatedRecipe = _mapper.Map<Recipe>(recipeDTO);
                updatedRecipe.CreatedBy = recipe.CreatedBy;
                updatedRecipe.CreatedAt = recipe.CreatedAt;

                await _dbRecipe.UpdateAsync(updatedRecipe);

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
    }
}
