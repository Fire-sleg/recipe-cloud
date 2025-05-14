using Microsoft.EntityFrameworkCore;
using RecipeService.Data;
using RecipeService.Models.Breadcrumbs;
using RecipeService.Models.Pagination;
using RecipeService.Models.Recipes;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace RecipeService.Repository
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly ICategoryRepository _dbCategory;
        private readonly IBreadcrumbRepository _dbBreadcrumb;
        public RecipeRepository(ApplicationDbContext db, ICategoryRepository dbCategory, IBreadcrumbRepository dbBreadcrumb)
        {
            _db = db;
            _dbCategory = dbCategory;
            _dbBreadcrumb = dbBreadcrumb;
        }

        public async Task CreateAsync(Recipe entity)
        {
            entity.TransliteratedName = Transliterator.Transliterate(entity.Title);

            await _db.Recipes.AddAsync(entity);
            await SaveAsync();

            var createdRecipe = await GetAsync(u => u.TransliteratedName == entity.TransliteratedName);



            List<BreadcrumbItem> breadcrumbPath = new List<BreadcrumbItem>();

            var parentCategory = await _dbCategory.GetAsync(c => c.Id == createdRecipe.CategoryId, withSubCategories: false, withRecipes: false, tracked: false);
            var breadcrumbItem = new BreadcrumbItem()
            {
                Name = parentCategory.Name,
                TransliteratedName = parentCategory.TransliteratedName,
                RecipeId = createdRecipe.Id,
            };
            breadcrumbPath.Add(breadcrumbItem);
            //await _dbBreadcrumb.CreateAsync(breadcrumbItem);

            while (parentCategory.ParentCategoryId != null)
            {
                parentCategory = await _dbCategory.GetAsync(c => c.Id == parentCategory.ParentCategoryId, withSubCategories: false, withRecipes: false, tracked: false);
                breadcrumbItem = new BreadcrumbItem()
                {
                    Name = parentCategory.Name,
                    TransliteratedName = parentCategory.TransliteratedName,
                    RecipeId = createdRecipe.Id
                };
                breadcrumbPath.Add(breadcrumbItem);
                //await _dbBreadcrumb.CreateAsync(breadcrumbItem);
            }

            breadcrumbPath.Reverse();
            int order = 1;
            foreach (var item in breadcrumbPath)
            {
                item.Order = order;
                order++;
                await _dbBreadcrumb.CreateAsync(item);
            }
        }
        public async Task<Recipe> UpdateAsync(Recipe entity)
        {
            //entity.UpdatedDate = DateTime.Now;
            _db.Recipes.Update(entity);
            await SaveAsync();
            return entity;
        }
        public async Task RemoveAsync(Recipe entity)
        {
            _db.Recipes.Remove(entity);
            await SaveAsync();
        }

        public async Task<List<Recipe>> GetAllAsync(Expression<Func<Recipe, bool>>? filter = null, PaginationParams paginationParams = null)
        {
            IQueryable<Recipe> query = _db.Recipes.Include(p => p.Category);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (paginationParams != null)
            {
                query = query
                    .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                    .Take(paginationParams.PageSize);
            }

            return await query.ToListAsync();
        }

        public async Task<Recipe> GetAsync(Expression<Func<Recipe, bool>>? filter = null, bool isTracked = true)
        {
            IQueryable<Recipe> query = _db.Recipes.Include(p => p.Category);

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

        public async Task<int> CountAsync(Expression<Func<Recipe, bool>>? filter = null)
        {
            IQueryable<Recipe> query = _db.Recipes;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.CountAsync();
        }

    }
}
