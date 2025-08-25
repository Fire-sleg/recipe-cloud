using RecipeService.Models.Collections;
using RecipeService.Models.Pagination;
using System.Linq.Expressions;

namespace RecipeService.Repository
{
    public interface ICollectionRepository
    {
        Task<List<Collection>> GetAllAsync(Expression<Func<Collection, bool>>? filter = null);
        Task<Collection> GetAsync(Expression<Func<Collection, bool>>? filter = null, bool isTracked = true);
        Task CreateAsync(Collection entity);
        Task<Collection> UpdateAsync(Collection entity);
        Task RemoveAsync(Collection entity);
        Task SaveAsync();
        Task<int> CountAsync(Expression<Func<Collection, bool>>? filter = null);
        Task<Collection?> AddRecipeToCollection(Guid collectionId, Guid recipeId);
        Task<Collection?> RemoveRecipeFromCollectionAsync(Guid collectionId, Guid recipeId);
    }
}
