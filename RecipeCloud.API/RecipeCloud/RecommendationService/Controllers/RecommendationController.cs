using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecommendationService.Models;
using RecommendationService.Services;
using System.Security.Claims;

namespace RecommendationService.Controllers
{
    [Route("api/recommendations")]
    [ApiController]
    [Authorize]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet]
        public async Task<ActionResult<List<RecipeDTO>>> GetRecommendations(int limit = 6)
        {
            var userId = Guid.Parse(User.FindFirst("ident")?.Value);
            var recommendations = await _recommendationService.GetRecommendations(userId, limit);
            if (recommendations.Metrics != null)
            {
                foreach (var pair in recommendations.Metrics)
                {
                    Console.WriteLine($"{pair.Key}: {pair.Value}");
                }
            }
            

            return Ok(recommendations.Recommendations);
        }
    }
}
