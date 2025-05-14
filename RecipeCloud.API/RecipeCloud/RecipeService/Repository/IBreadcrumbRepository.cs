using RecipeService.Models.Breadcrumbs;
using System.Linq.Expressions;

namespace RecipeService.Repository
{
    public interface IBreadcrumbRepository
    {
        Task<List<BreadcrumbItem>> GetAllAsync(Expression<Func<BreadcrumbItem, bool>>? filter = null);
        Task<BreadcrumbItem> GetAsync(Expression<Func<BreadcrumbItem, bool>> filter = null, bool tracked = true);
        Task CreateAsync(BreadcrumbItem entity);
        Task RemoveAsync(BreadcrumbItem entity);
        Task<BreadcrumbItem> UpdateAsync(BreadcrumbItem entity);
        Task SaveAsync();
    }
}
