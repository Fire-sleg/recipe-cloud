using RecipeService.Models.Breadcrumbs;
using System.Linq.Expressions;

namespace RecipeService.Repository
{
    public interface IBreadcrumbRepository
    {
        Task<List<BreadcrumbItem>> GetAllAsync(Expression<Func<BreadcrumbItem, bool>>? filter = null, CancellationToken cancellationToken = default);
        Task<BreadcrumbItem> GetAsync(Expression<Func<BreadcrumbItem, bool>> filter = null, bool tracked = true, CancellationToken cancellationToken = default);
        Task CreateAsync(BreadcrumbItem entity, CancellationToken cancellationToken = default);
        Task RemoveAsync(BreadcrumbItem entity, CancellationToken cancellationToken = default);
        Task<BreadcrumbItem> UpdateAsync(BreadcrumbItem entity, CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);
    }
}
