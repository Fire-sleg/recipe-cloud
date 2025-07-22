using RecommendationService.Models;

namespace RecommendationService.Services
{
    public interface IRecommendationService
    {
        Task<RecommendationResult> GetRecommendations(Guid userId, int limit);
    }

}
