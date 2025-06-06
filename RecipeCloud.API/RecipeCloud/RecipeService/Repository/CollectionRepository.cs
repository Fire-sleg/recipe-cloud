﻿using Microsoft.EntityFrameworkCore;
using RecipeService.Data;
using RecipeService.Models.Collections;
using RecipeService.Models.Pagination;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RecipeService.Repository
{
    public class CollectionRepository : ICollectionRepository
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<Collection> _dbSet;

        public CollectionRepository(ApplicationDbContext db)
        {
            _db = db;
            _dbSet = db.Set<Collection>();
        }

        public async Task CreateAsync(Collection entity)
        {
            await _dbSet.AddAsync(entity);
            await SaveAsync();
        }

        public async Task<Collection> UpdateAsync(Collection entity)
        {
            _dbSet.Update(entity);
            await SaveAsync();
            return entity;
        }

        public async Task RemoveAsync(Collection entity)
        {
            _dbSet.Remove(entity);
            await SaveAsync();
        }

        public async Task<List<Collection>> GetAllAsync(Expression<Func<Collection, bool>>? filter = null)
        {
            IQueryable<Collection> query = _dbSet.Include(c => c.Recipes);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync();
        }

        public async Task<Collection> GetAsync(Expression<Func<Collection, bool>>? filter = null, bool isTracked = true)
        {
            IQueryable<Collection> query = _dbSet.Include(c => c.Recipes);

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

        public async Task<int> CountAsync(Expression<Func<Collection, bool>>? filter = null)
        {
            IQueryable<Collection> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.CountAsync();
        }
    }
}