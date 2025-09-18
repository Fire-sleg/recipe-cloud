using RecipeService.Models.Collections;
using RecipeService.Models.Pagination;
using System.Linq.Expressions;

namespace RecipeService.Repository
{
    public interface ICollectionRepository
    {
        Task<List<Collection>> GetAllAsync(Expression<Func<Collection, bool>>? filter = null, CancellationToken cancellationToken = default);
        Task<Collection> GetAsync(Expression<Func<Collection, bool>>? filter = null, bool isTracked = true, CancellationToken cancellationToken = default);
        Task CreateAsync(Collection entity, CancellationToken cancellationToken = default);
        Task<Collection> UpdateAsync(Collection entity, CancellationToken cancellationToken = default);
        Task RemoveAsync(Collection entity, CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<Collection, bool>>? filter = null, CancellationToken cancellationToken = default);
        Task<Collection?> AddRecipeToCollection(Guid collectionId, Guid recipeId, CancellationToken cancellationToken = default);
        Task<Collection?> RemoveRecipeFromCollectionAsync(Guid collectionId, Guid recipeId, CancellationToken cancellationToken = default);
    }
}
