using Microsoft.EntityFrameworkCore;
using RecipeService.Data;
using RecipeService.Models.Rating;
using RecipeService.Models.Rating.DTOs;
using System.Threading;

namespace RecipeService.Repository
{
    public class RecipeRatingRepository : IRecipeRatingRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IRecipeRepository _dbRecipe;

        public RecipeRatingRepository(ApplicationDbContext db, IRecipeRepository dbRecipe)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(dbRecipe);

            _db = db;
            _dbRecipe = dbRecipe;
        }

        public async Task<double> GetAverageRatingAsync(Guid recipeId, CancellationToken cancellationToken = default)
        {
            var average = await _db.RecipeRatings
                .Where(r => r.RecipeId == recipeId)
                .AsNoTracking()
                .AverageAsync(r => r.Rating, cancellationToken);

            return average;
        }

        public Task SaveAsync(CancellationToken cancellationToken = default) =>
            _db.SaveChangesAsync(cancellationToken);

        public async Task<bool> RateRecipeAsync(Guid userId, Guid recipeId, int rating, CancellationToken cancellationToken = default)
        {
            // Check if user has already rated this recipe
            var existingRating = await _db.RecipeRatings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.RecipeId == recipeId, cancellationToken);

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.Rating = rating;
                existingRating.RatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new rating
                var newRating = new RecipeRating
                {
                    UserId = userId,
                    RecipeId = recipeId,
                    Rating = rating,
                    RatedAt = DateTime.UtcNow
                };

                await _db.RecipeRatings.AddAsync(newRating, cancellationToken);
            }

            await SaveAsync(cancellationToken);

            // Update recipe's average rating
            var averageRating = await GetAverageRatingAsync(recipeId, cancellationToken);
            var recipe = await _dbRecipe.GetAsync(r => r.Id == recipeId, isTracked: true, cancellationToken: cancellationToken);
            if (recipe == null)
            {
                return false; // Recipe not found
            }

            recipe.AverageRating = averageRating;
            await _dbRecipe.UpdateAsync(recipe, cancellationToken);

            return true;
        }

        public async Task<RecipeRatingDTO?> GetRecipeRatingAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken = default)
        {
            var existingRating = await _db.RecipeRatings
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.UserId == userId && r.RecipeId == recipeId, cancellationToken);

            if (existingRating == null)
            {
                return null;
            }

            return new RecipeRatingDTO
            {
                RecipeId = existingRating.RecipeId,
                Rating = existingRating.Rating
            };
        }

        public async Task<List<RecipeRating>> GetUserRatingsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _db.RecipeRatings
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .ToListAsync(cancellationToken);
        }
    }
}