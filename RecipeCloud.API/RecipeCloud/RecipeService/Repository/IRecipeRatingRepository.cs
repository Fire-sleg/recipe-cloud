using RecipeService.Models.Rating;
using RecipeService.Models.Rating.DTOs;

namespace RecipeService.Repository
{
    public interface IRecipeRatingRepository
    {
        Task<bool> RateRecipeAsync(Guid userId, Guid recipeId, int rating);
        Task<double> GetAverageRatingAsync(Guid recipeId);
        Task<RecipeRatingDTO> GetRecipeRating(Guid recipeId, Guid userId);
        Task<List<RecipeRating>> GetUserRatingAsync(Guid userId);
    }
}
