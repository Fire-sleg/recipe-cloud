using Microsoft.EntityFrameworkCore;
using RecipeService.Data;
using RecipeService.Models.Collections;
using System.Linq.Expressions;
using System.Threading;

namespace RecipeService.Repository
{
    public class CollectionRepository : ICollectionRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly DbSet<Collection> _dbSet;
        private readonly IRecipeRepository _dbRecipe;

        public CollectionRepository(ApplicationDbContext db, IRecipeRepository dbRecipe)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(dbRecipe);

            _db = db;
            _dbSet = db.Set<Collection>();
            _dbRecipe = dbRecipe;
        }

        public async Task CreateAsync(Collection entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            await _dbSet.AddAsync(entity, cancellationToken);
            await SaveAsync(cancellationToken);
        }

        public async Task<Collection> UpdateAsync(Collection entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _dbSet.Update(entity);
            await SaveAsync(cancellationToken);
            return entity;
        }

        public async Task RemoveAsync(Collection entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _dbSet.Remove(entity);
            await SaveAsync(cancellationToken);
        }

        public async Task<List<Collection>> GetAllAsync(
            Expression<Func<Collection, bool>>? filter = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Collection> query = _dbSet
                .Include(c => c.Recipes)
                .AsSplitQuery();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<Collection?> GetAsync(
            Expression<Func<Collection, bool>>? filter = null,
            bool isTracked = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Collection> query = _dbSet
                .Include(c => c.Recipes)
                .AsSplitQuery();

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

        public Task SaveAsync(CancellationToken cancellationToken = default) =>
            _db.SaveChangesAsync(cancellationToken);

        public async Task<int> CountAsync(
            Expression<Func<Collection, bool>>? filter = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Collection> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task<Collection?> AddRecipeToCollection(Guid collectionId, Guid recipeId, CancellationToken cancellationToken = default)
        {
            var collection = await GetAsync(c => c.Id == collectionId, isTracked: true, cancellationToken: cancellationToken);
            if (collection == null)
            {
                return null;
            }

            var recipe = await _dbRecipe.GetAsync(r => r.Id == recipeId, isTracked: false, cancellationToken: cancellationToken);
            if (recipe == null)
            {
                return null;
            }

            // Check if recipe is already in collection to avoid duplicates
            if (collection.Recipes.Any(r => r.Id == recipeId))
            {
                return collection;
            }

            collection.Recipes.Add(recipe);
            await UpdateAsync(collection, cancellationToken);
            return collection;
        }

        public async Task<Collection?> RemoveRecipeFromCollectionAsync(Guid collectionId, Guid recipeId, CancellationToken cancellationToken = default)
        {
            var collection = await GetAsync(c => c.Id == collectionId, isTracked: true, cancellationToken: cancellationToken);
            if (collection == null)
            {
                return null;
            }

            var recipe = collection.Recipes.FirstOrDefault(r => r.Id == recipeId);
            if (recipe == null)
            {
                return collection; // Recipe not in collection, return unchanged
            }

            collection.Recipes.Remove(recipe);
            await UpdateAsync(collection, cancellationToken);
            return collection;
        }

    }
}