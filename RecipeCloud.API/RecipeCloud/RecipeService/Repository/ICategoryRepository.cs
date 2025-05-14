using RecipeService.Models.Categories;
using System.Linq.Expressions;

namespace RecipeService.Repository
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(Expression<Func<Category, bool>>? filter = null, bool withSubCategories = true, bool withRecipes = true);
        Task<Category> GetAsync(Expression<Func<Category, bool>> filter = null, bool withSubCategories = true, bool withRecipes = true, bool tracked = true);
        Task CreateAsync(Category entity);
        Task RemoveAsync(Category entity);
        Task<Category> UpdateAsync(Category entity);
        Task SaveAsync();
    }
}
