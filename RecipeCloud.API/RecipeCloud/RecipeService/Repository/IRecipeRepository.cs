using RecipeService.Models.Filter;
using RecipeService.Models.Pagination;
using RecipeService.Models.Recipes;
using RecipeService.Models.Recipes.DTOs;
using System.Linq.Expressions;

namespace RecipeService.Repository
{
    public interface IRecipeRepository
    {
        Task<List<Recipe>> GetAllAsync(Expression<Func<Recipe, bool>>? filter = null, PaginationParams? paginationParams = null);
        Task<Recipe> GetAsync(Expression<Func<Recipe, bool>>? filter = null, bool isTracked = true);
        Task CreateAsync(Recipe entity);
        Task<Recipe> UpdateAsync(Recipe entity);
        Task RemoveAsync(Recipe entity);
        Task SaveAsync();
        Task<int> CountAsync(Expression<Func<Recipe, bool>>? filter = null);
        Task<List<Recipe>> FilterRecipesAsync(RecipeFilterDTO filterDto, PaginationParams? paginationParams = null, string? sortOrder = null);
        Task<bool> IncrementViewCountAsync(Guid recipeId);
    }
}
