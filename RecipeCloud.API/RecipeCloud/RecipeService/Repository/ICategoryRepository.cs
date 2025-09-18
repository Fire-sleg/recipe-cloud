using RecipeService.Models.Categories;
using System.Linq.Expressions;

namespace RecipeService.Repository
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(Expression<Func<Category, bool>>? filter = null, bool withSubCategories = true, bool withRecipes = true, CancellationToken cancellationToken = default);
        Task<Category> GetAsync(Expression<Func<Category, bool>> filter = null, bool withSubCategories = true, bool withRecipes = true, bool tracked = true, CancellationToken cancellationToken = default);
        Task CreateAsync(Category entity, CancellationToken cancellationToken = default);
        Task RemoveAsync(Category entity, CancellationToken cancellationToken = default);
        Task<Category> UpdateAsync(Category entity, CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);
    }
}
