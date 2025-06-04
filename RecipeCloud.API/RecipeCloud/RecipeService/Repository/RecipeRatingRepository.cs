using Microsoft.EntityFrameworkCore;
using Nest;
using RecipeService.Data;
using RecipeService.Models.Rating;
using RecipeService.Models.Rating.DTOs;

namespace RecipeService.Repository
{
    public class RecipeRatingRepository : IRecipeRatingRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IRecipeRepository _dbRecipe;

        public RecipeRatingRepository(ApplicationDbContext db, IRecipeRepository dbRecipe)
        {
            _db = db;
            _dbRecipe = dbRecipe;
        }

        public async Task<double> GetAverageRatingAsync(Guid recipeId)
        {
            var ratings = await _db.RecipeRatings
                .Where(r => r.RecipeId == recipeId)
                .ToListAsync();

            if (!ratings.Any())
            {
                return 0;
            }

            var averageRating = ratings.Average(r => r.Rating);
           

            return averageRating;
        }
        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task<bool> RateRecipeAsync(Guid userId, Guid recipeId, int rating)
        {
            try
            {
                // Перевіряємо, чи користувач вже оцінив цей рецепт
                var existingRating = await _db.RecipeRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.RecipeId == recipeId);

                if (existingRating != null)
                {
                    // Оновлюємо існуючий рейтинг
                    existingRating.Rating = rating;
                    existingRating.RatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Створюємо новий рейтинг
                    var newRating = new RecipeRating
                    {
                        UserId = userId,
                        RecipeId = recipeId,
                        Rating = rating,
                        RatedAt = DateTime.UtcNow
                    };

                    await _db.RecipeRatings.AddAsync(newRating);

                }

                var averageRating = GetAverageRatingAsync(recipeId);
                var recipe = await _dbRecipe.GetAsync(r => r.Id == recipeId);
                recipe.AverageRating = averageRating.Result;
                await _dbRecipe.SaveAsync();

                await SaveAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<RecipeRatingDTO> GetRecipeRating(Guid recipeId, Guid userId)
        {
            try
            {
                // Перевіряємо, чи користувач вже оцінив цей рецепт
                var existingRating = await _db.RecipeRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.RecipeId == recipeId);

                if (existingRating != null)
                {
                    var ratingDTO = new RecipeRatingDTO
                    {
                        RecipeId = existingRating.RecipeId,
                        Rating = existingRating.Rating
                    };
                    return ratingDTO;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<RecipeRating>> GetUserRatingAsync(Guid userId)
        {
            try
            {
                // Перевіряємо, чи користувач вже оцінив цей рецепт
                var userRatings = await _db.RecipeRatings.Where(r => r.UserId == userId).ToListAsync();

                if (userRatings != null)
                {
                    return userRatings;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
