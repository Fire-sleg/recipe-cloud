using RecipeService.Models;
using RecipeService.Models.DTOs;
using System.Linq.Expressions;

namespace RecipeService.Repository
{
    public interface IRecipeRepository
    {
        Task<List<Recipe>> GetAllAsync(Expression<Func<Recipe, bool>>? filter = null);
        Task<Recipe> GetAsync(Expression<Func<Recipe, bool>>? filter = null, bool isTracked = true);
        Task CreateAsync(Recipe entity);
        Task<Recipe> UpdateAsync(Recipe entity);
        Task RemoveAsync(Recipe entity);
        Task SaveAsync();

    }
}
