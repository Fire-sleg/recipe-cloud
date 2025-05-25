using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecommendationService.Services;
using System.Security.Claims;

namespace RecommendationService.Controllers
{
    [Route("api/recommendations")]
    [ApiController]
    [Authorize]
    public class RecommendationController : ControllerBase
    {
        private readonly RecommendationMainService _recommendationService;

        public RecommendationController(RecommendationMainService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet]
        public async Task<ActionResult<List<object>>> GetRecommendations(int limit = 6)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var recommendations = await _recommendationService.GetRecommendations(userId, limit);
            return Ok(recommendations);
        }
    }
}
