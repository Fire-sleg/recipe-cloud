using RecipeService.Models.Filter;
using RecipeService.Models.Pagination;
using RecipeService.Models.Recipes;
using RecipeService.Models.Recipes.DTOs;
using System.Linq.Expressions;

namespace RecipeService.Repository
{
    public interface IRecipeRepository
    {
        Task<List<Recipe>> GetAllAsync(
            Expression<Func<Recipe, bool>>? filter = null, 
            PaginationParams? paginationParams = null, 
            CancellationToken cancellationToken = default);

        Task<Recipe> GetAsync(
            Expression<Func<Recipe, bool>>? filter = null, 
            bool isTracked = true, 
            CancellationToken cancellationToken = default);

        Task CreateAsync(Recipe entity, CancellationToken cancellationToken = default);
        Task<Recipe> UpdateAsync(Recipe entity, CancellationToken cancellationToken = default);
        Task RemoveAsync(Recipe entity, CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);

        Task<int> CountAsync(
            Expression<Func<Recipe, bool>>? filter = null, 
            CancellationToken cancellationToken = default);

        Task<List<Recipe>> FilterRecipesAsync(
            RecipeFilterDTO filterDto, 
            PaginationParams? paginationParams = null, 
            string? sortOrder = null, 
            CancellationToken cancellationToken = default);

        Expression<Func<Recipe, bool>> GetFilterExpression(RecipeFilterDTO filterDto);
        Task<bool> IncrementViewCountAsync(Guid recipeId, CancellationToken cancellationToken = default);

        Task<(List<Recipe> Recipes, int TotalCount)> FilterWithCountAsync(
            Expression<Func<Recipe, bool>> filter,
            PaginationParams paginationParams,
            string? sortOrder,
            CancellationToken cancellationToken = default);
    }
}
