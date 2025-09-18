using Microsoft.EntityFrameworkCore;
using RecipeService.Data;
using RecipeService.Models.Breadcrumbs;
using System.Linq.Expressions;
using System.Threading;

namespace RecipeService.Repository
{
    public class BreadcrumbRepository : IBreadcrumbRepository
    {
        private readonly ApplicationDbContext _db;

        public BreadcrumbRepository(ApplicationDbContext db)
        {
            ArgumentNullException.ThrowIfNull(db);

            _db = db;
        }

        public async Task CreateAsync(BreadcrumbItem entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await _db.Breadcrumbs.AddAsync(entity, cancellationToken);
            await SaveAsync(cancellationToken);
        }

        public async Task<List<BreadcrumbItem>> GetAllAsync(
            Expression<Func<BreadcrumbItem, bool>>? filter = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<BreadcrumbItem> query = _db.Breadcrumbs;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<BreadcrumbItem?> GetAsync(
            Expression<Func<BreadcrumbItem, bool>>? filter = null,
            bool isTracked = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<BreadcrumbItem> query = _db.Breadcrumbs;

            if (!isTracked)
            {
                query = query.AsNoTracking();
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task RemoveAsync(BreadcrumbItem entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _db.Breadcrumbs.Remove(entity);
            await SaveAsync(cancellationToken);
        }

        public Task SaveAsync(CancellationToken cancellationToken = default) =>
            _db.SaveChangesAsync(cancellationToken);

        public async Task<BreadcrumbItem> UpdateAsync(BreadcrumbItem entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _db.Breadcrumbs.Update(entity);
            await SaveAsync(cancellationToken);
            return entity;
        }
    }
}