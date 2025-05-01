using Microsoft.EntityFrameworkCore;
using RecipeService.Data;
using RecipeService.Models;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace RecipeService.Repository
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<Recipe> _dbSet;
        public RecipeRepository(ApplicationDbContext db)
        {
            _db = db;
            _dbSet = db.Set<Recipe>();
        }

        public async Task CreateAsync(Recipe entity)
        {
            await _dbSet.AddAsync(entity);
            await SaveAsync();
        }
        public async Task<Recipe> UpdateAsync(Recipe entity)
        {
            //entity.UpdatedDate = DateTime.Now;
            _dbSet.Update(entity);
            await _db.SaveChangesAsync();
            return entity;
        }
        public async Task RemoveAsync(Recipe entity)
        {
            _dbSet.Remove(entity);
            await SaveAsync();
        }

        public async Task<List<Recipe>> GetAllAsync(Expression<Func<Recipe, bool>>? filter = null)
        {
            IQueryable<Recipe> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync();
        }

        public async Task<Recipe> GetAsync(Expression<Func<Recipe, bool>>? filter = null, bool isTracked = true)
        {
            IQueryable<Recipe> query = _dbSet;

            if (!isTracked)
            {
                query = query.AsNoTracking();
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.FirstOrDefaultAsync();
        }

       

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

       
    }
}
