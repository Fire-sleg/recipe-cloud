using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RecipeService.Models.Breadcrumbs;
using RecipeService.Repository;

namespace RecipeService.Controllers
{
    [Route("api/breadcrumb")]
    [ApiController]
    public class BreadcrumbController : ControllerBase
    {
        private readonly IBreadcrumbRepository _breadcrumbRepository;
        private readonly ILogger<BreadcrumbController> _logger;

        public BreadcrumbController(IBreadcrumbRepository breadcrumbRepository, ILogger<BreadcrumbController> logger)
        {
            _breadcrumbRepository = breadcrumbRepository;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all breadcrumb items.
        /// </summary>
        /// <returns>A list of all breadcrumb items.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<BreadcrumbItem>))]
        public async Task<IActionResult> GetBreadcrumbsAsync()
        {
            _logger.LogInformation("Fetching all breadcrumbs.");
            var breadcrumbs = await _breadcrumbRepository.GetAllAsync();
            return Ok(breadcrumbs);
        }

        /// <summary>
        /// Retrieves a specific breadcrumb item by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the breadcrumb item.</param>
        /// <returns>The breadcrumb item if found; otherwise, NotFound.</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BreadcrumbItem))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAsync(Guid id)
        {
            _logger.LogInformation("Fetching breadcrumb with ID: {Id}.", id);
            var breadcrumb = await _breadcrumbRepository.GetAsync(u => u.Id == id);
            if (breadcrumb == null)
            {
                _logger.LogWarning("Breadcrumb with ID {Id} not found.", id);
                return NotFound();
            }
            return Ok(breadcrumb);
        }

        /// <summary>
        /// Creates a new breadcrumb item.
        /// </summary>
        /// <param name="createDTO">The breadcrumb item data to create.</param>
        /// <returns>NoContent if successful; BadRequest if input is invalid.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAsync([FromBody] BreadcrumbItem createDTO)
        {
            if (createDTO == null || !ModelState.IsValid)
            {
                _logger.LogWarning("Invalid breadcrumb creation attempt.");
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating new breadcrumb.");
            await _breadcrumbRepository.CreateAsync(createDTO);
            return NoContent();
        }

        /// <summary>
        /// Deletes a breadcrumb item by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the breadcrumb item to delete.</param>
        /// <returns>NoContent if successful; BadRequest if ID is invalid; NotFound if item does not exist.</returns>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("Invalid breadcrumb ID for deletion: {Id}.", id);
                return BadRequest("Invalid ID.");
            }

            _logger.LogInformation("Fetching breadcrumb for deletion with ID: {Id}.", id);
            var breadcrumb = await _breadcrumbRepository.GetAsync(u => u.Id == id);

            if (breadcrumb == null)
            {
                _logger.LogWarning("Breadcrumb with ID {Id} not found for deletion.", id);
                return NotFound();
            }

            _logger.LogInformation("Deleting breadcrumb with ID: {Id}.", id);
            await _breadcrumbRepository.RemoveAsync(breadcrumb);
            return NoContent();
        }

        /// <summary>
        /// Updates an existing breadcrumb item.
        /// </summary>
        /// <param name="id">The unique identifier of the breadcrumb item to update.</param>
        /// <param name="updateDTO">The updated breadcrumb item data.</param>
        /// <returns>NoContent if successful; BadRequest if input is invalid; InternalServerError on exception.</returns>
        [HttpPut("{id:guid}", Name = "UpdateBreadcrumb")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateBreadcrumbAsync(Guid id, [FromBody] BreadcrumbItem updateDTO)
        {
            if (updateDTO == null || id != updateDTO.Id || !ModelState.IsValid)
            {
                _logger.LogWarning("Invalid breadcrumb update attempt for ID: {Id}.", id);
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Updating breadcrumb with ID: {Id}.", id);
            await _breadcrumbRepository.UpdateAsync(updateDTO);
            return NoContent();
        }

        /// <summary>
        /// Partially updates a breadcrumb item using a JSON Patch document.
        /// </summary>
        /// <param name="id">The unique identifier of the breadcrumb item to update.</param>
        /// <param name="patchDTO">The JSON Patch document containing the updates.</param>
        /// <returns>NoContent if successful; BadRequest if input or model state is invalid.</returns>
        [HttpPatch("{id:guid}", Name = "UpdatePartialBreadcrumb")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePartialBreadcrumbAsync(Guid id, [FromBody] JsonPatchDocument<BreadcrumbItem> patchDTO)
        {
            if (patchDTO == null)
            {
                _logger.LogWarning("Invalid patch document for breadcrumb ID: {Id}.", id);
                return BadRequest("Invalid patch document.");
            }

            _logger.LogInformation("Fetching breadcrumb for partial update with ID: {Id}.", id);
            var breadcrumb = await _breadcrumbRepository.GetAsync(u => u.Id == id, tracked: false);

            if (breadcrumb == null)
            {
                _logger.LogWarning("Breadcrumb with ID {Id} not found for partial update.", id);
                return NotFound();
            }

            patchDTO.ApplyTo(breadcrumb, ModelState);

            if (!TryValidateModel(breadcrumb))
            {
                _logger.LogWarning("Model state invalid after applying patch for breadcrumb ID: {Id}.", id);
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Applying partial update to breadcrumb with ID: {Id}.", id);
            await _breadcrumbRepository.UpdateAsync(breadcrumb);

            return NoContent();
        }
    }
}