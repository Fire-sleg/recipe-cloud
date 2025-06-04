using AuthService.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [Route("api/view-history")]
    [ApiController]
    public class ViewHistoryController : ControllerBase
    {
        private readonly IViewHistoryRepository _dbViewHistory;
        public ViewHistoryController(IViewHistoryRepository dbViewHistory)
        {
            _dbViewHistory = dbViewHistory;
        }
        [Authorize]
        [HttpGet("{limit:int}")]
        public async Task<IActionResult> Get(int limit)
        {
            // Отримуємо UserId з токена
            var userId = GetCurrentUserId();
            var viewHistories = await _dbViewHistory.GetViewHistory(userId, 10);

            return Ok(viewHistories);
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendViewHistoryAsync([FromBody] ViewHistoryDto viewHistoryDto)
        {
            try
            {
                // Отримуємо UserId з токена
                var userId = GetCurrentUserId();

                await _dbViewHistory.SendViewHistoryAsync(viewHistoryDto.RecipeId, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }
        private Guid GetCurrentUserId()
        {
            // Припускаю, що UserId зберігається в claims
            var userId = Guid.Parse(User.FindFirst("ident")?.Value);
            return userId;
        }
    }

    public class ViewHistoryDto
    {
        public Guid RecipeId { get; set; }
    }
}
