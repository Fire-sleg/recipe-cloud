using Microsoft.EntityFrameworkCore;
using RecipeService.Data;
using RecipeService.Models.Breadcrumbs;
using System.Linq.Expressions;
using System.Linq;

namespace RecipeService.Repository
{
    public class BreadcrumbRepository : IBreadcrumbRepository
    {
        private readonly ApplicationDbContext _db;
        public BreadcrumbRepository(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task CreateAsync(BreadcrumbItem entity)
        {
            await _db.Breadcrumbs.AddAsync(entity);
            await SaveAsync();
        }

        public async Task<List<BreadcrumbItem>> GetAllAsync(Expression<Func<BreadcrumbItem, bool>>? filter = null)
        {
            IQueryable<BreadcrumbItem> query = _db.Breadcrumbs;

            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.ToListAsync();
        }

        public async Task<BreadcrumbItem> GetAsync(Expression<Func<BreadcrumbItem, bool>> filter = null, bool tracked = true)
        {
            IQueryable<BreadcrumbItem> query = _db.Breadcrumbs;


            if (!tracked)
            {
                query = query.AsNoTracking();
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task RemoveAsync(BreadcrumbItem entity)
        {
            _db.Breadcrumbs.Remove(entity);
            await SaveAsync();
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task<BreadcrumbItem> UpdateAsync(BreadcrumbItem entity)
        {
            _db.Breadcrumbs.Update(entity);
            await SaveAsync();
            return entity;
        }
    }
}
