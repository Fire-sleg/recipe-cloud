using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RecipeService.Models.Categories.DTOs;
using RecipeService.Models.Categories;
using RecipeService.Repository;

namespace RecipeService.Controllers
{
    [Route("api/categories/")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _dbCategory;
        private readonly IMapper _mapper;
        public CategoryController(ICategoryRepository dbCategory, IMapper mapper)
        {
            _dbCategory = dbCategory;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoriesAsync()
        {
            var list = await _dbCategory.GetAllAsync();
            var categories = _mapper.Map<List<CategoryDTO>>(list);
            return Ok(categories);
        }

        [HttpGet("base")]
        public async Task<IActionResult> GetBaseCategoriesAsync()
        {
            var list = await _dbCategory.GetAllAsync(
                c => c.ParentCategoryId == null || c.ParentCategoryId == Guid.Empty,
                withSubCategories: true, 
                withRecipes: false
            );

            var categories = _mapper.Map<List<CategoryDTO>>(list);
            return Ok(categories);
        }

        

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetAsync(Guid id)
        {
            var model = await _dbCategory.GetAsync(u => u.Id == id);
            var category = _mapper.Map<CategoryDTO>(model);
            return Ok(category);
        }

        [HttpGet("{transliteratedName}")]
        public async Task<IActionResult> GetByTransliteratedNameAsync(string transliteratedName)
        {
            var model = await _dbCategory.GetAsync(u => u.TransliteratedName == transliteratedName);
            var category = _mapper.Map<CategoryDTO>(model);
            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] CategoryCreateDTO createDTO)
        {
            if (createDTO == null)
            {
                return BadRequest();
            }

            if (await _dbCategory.GetAsync(
                u => u.Name.ToLower() == createDTO.Name.ToLower(), 
                withSubCategories: false, 
                withRecipes: false, 
                tracked: false) != null)
            {
                return BadRequest();
            }

            Category model = _mapper.Map<Category>(createDTO);
            await _dbCategory.CreateAsync(model);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var category = await _dbCategory.GetAsync(u => u.Id == id);

            if (category == null)
            {
                return NotFound();
            }
            await _dbCategory.RemoveAsync(category);
            return NoContent();
        }
        [HttpPut("{id:guid}", Name = "UpdateCategory")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCategoryAsync(Guid id, [FromBody] CategoryUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null || id != updateDTO.Id)
                {
                    return BadRequest();
                }

                Category model = _mapper.Map<Category>(updateDTO);

                await _dbCategory.UpdateAsync(model);

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return StatusCode(StatusCodes.Status500InternalServerError);

        }
        [HttpPatch("{id:guid}", Name = "UpdatePartialCategory")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePartialCategoryAsync(Guid id, JsonPatchDocument<CategoryUpdateDTO> patchDTO)
        {
            if (patchDTO == null)
            {
                return BadRequest();
            }
            /*---AsNoTracking()---*/
            var category = await _dbCategory.GetAsync(u => u.Id == id, tracked: false);

            CategoryUpdateDTO messageDTO = _mapper.Map<CategoryUpdateDTO>(category);

            if (category == null)
            {
                return BadRequest();
            }

            patchDTO.ApplyTo(messageDTO, ModelState);

            Category model = _mapper.Map<Category>(messageDTO);


            await _dbCategory.UpdateAsync(model);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            return NoContent();
        }
    }
}
