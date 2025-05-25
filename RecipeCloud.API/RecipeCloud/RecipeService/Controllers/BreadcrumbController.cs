using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using RecipeService.Models.Breadcrumbs;
using RecipeService.Repository;

namespace RecipeService.Controllers
{
    [Route("api/breadcrumb")]
    [ApiController]
    public class BreadcrumbController : ControllerBase
    {
        private readonly IBreadcrumbRepository _dbBredcrumb;
        public BreadcrumbController(IBreadcrumbRepository dbBredcrumb)
        {
            _dbBredcrumb = dbBredcrumb;
        }
        [HttpGet]
        public async Task<IActionResult> GetBreadcrumbsAsync()
        {
            var list = await _dbBredcrumb.GetAllAsync();
            return Ok(list);
        }


        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetAsync(Guid id)
        {
            var model = await _dbBredcrumb.GetAsync(u => u.Id == id);
            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] BreadcrumbItem createDTO)
        {
            if (createDTO == null)
            {
                return BadRequest();
            }

            await _dbBredcrumb.CreateAsync(createDTO);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var breadcrumb = await _dbBredcrumb.GetAsync(u => u.Id == id);

            if (breadcrumb == null)
            {
                return NotFound();
            }
            await _dbBredcrumb.RemoveAsync(breadcrumb);
            return NoContent();
        }
        [HttpPut("{id:guid}", Name = "UpdateBreadcrumb")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateBreadcrumbAsync(Guid id, [FromBody] BreadcrumbItem updateDTO)
        {
            try
            {
                if (updateDTO == null || id != updateDTO.Id)
                {
                    return BadRequest();
                }

                await _dbBredcrumb.UpdateAsync(updateDTO);

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return StatusCode(StatusCodes.Status500InternalServerError);

        }
        [HttpPatch("{id:guid}", Name = "UpdatePartialBredcrumb")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePartialBredcrumbAsync(Guid id, JsonPatchDocument<BreadcrumbItem> patchDTO)
        {
            if (patchDTO == null)
            {
                return BadRequest();
            }
            /*---AsNoTracking()---*/
            var bredcrumb = await _dbBredcrumb.GetAsync(u => u.Id == id, tracked: false);

            if (bredcrumb == null)
            {
                return BadRequest();
            }

            patchDTO.ApplyTo(bredcrumb, ModelState);

            await _dbBredcrumb.UpdateAsync(bredcrumb);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            return NoContent();
        }
    }
}
