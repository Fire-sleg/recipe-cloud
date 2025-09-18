using RecipeService.Models.Rating;
using RecipeService.Models.Rating.DTOs;

namespace RecipeService.Repository
{
    public interface IRecipeRatingRepository
    {
        Task<bool> RateRecipeAsync(Guid userId, Guid recipeId, int rating, CancellationToken cancellationToken = default);
        Task<double> GetAverageRatingAsync(Guid recipeId, CancellationToken cancellationToken = default);
        Task<RecipeRatingDTO> GetRecipeRatingAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken = default);
        Task<List<RecipeRating>> GetUserRatingsAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
