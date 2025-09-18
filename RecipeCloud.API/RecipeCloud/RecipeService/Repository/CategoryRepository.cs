using Microsoft.EntityFrameworkCore;
using RecipeService.Data;
using RecipeService.Models.Breadcrumbs;
using RecipeService.Models.Categories;
using System.Linq.Expressions;
using System.Threading;

namespace RecipeService.Repository
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IBreadcrumbRepository _dbBreadcrumb;

        public CategoryRepository(ApplicationDbContext db, IBreadcrumbRepository dbBreadcrumb)
        {
            ArgumentNullException.ThrowIfNull(db);
            ArgumentNullException.ThrowIfNull(dbBreadcrumb);

            _db = db;
            _dbBreadcrumb = dbBreadcrumb;
        }

        public async Task<List<Category>> GetAllAsync(
            Expression<Func<Category, bool>>? filter = null,
            bool withSubCategories = true,
            bool withRecipes = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Category> query = _db.Categories
                .Include(c => c.BreadcrumbPath)
                .AsSplitQuery();

            if (withSubCategories)
            {
                query = query.Include(c => c.SubCategories);
            }

            if (withRecipes)
            {
                query = query.Include(c => c.Recipes);
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<Category?> GetAsync(
            Expression<Func<Category, bool>>? filter = null,
            bool withSubCategories = true,
            bool withRecipes = true,
            bool isTracked = true,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Category> query = _db.Categories
                .Include(c => c.BreadcrumbPath)
                .AsSplitQuery();

            if (withSubCategories)
            {
                query = query.Include(c => c.SubCategories);
            }

            if (withRecipes)
            {
                query = query.Include(c => c.Recipes);
            }

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

        public async Task CreateAsync(Category category, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(category);

            category.TransliteratedName = Transliterator.Transliterate(category.Name);

            await _db.Categories.AddAsync(category, cancellationToken);
            await SaveAsync(cancellationToken);

            List<BreadcrumbItem> breadcrumbPath = new List<BreadcrumbItem>();

            var parentCategory = await GetAsync(
                c => c.Id == category.ParentCategoryId,
                withSubCategories: false,
                withRecipes: false,
                isTracked: false,
                cancellationToken: cancellationToken);

            while (parentCategory != null)
            {
                var breadcrumbItem = new BreadcrumbItem
                {
                    Name = parentCategory.Name,
                    TransliteratedName = parentCategory.TransliteratedName,
                    CategoryId = category.Id
                };
                breadcrumbPath.Add(breadcrumbItem);

                if (parentCategory.ParentCategoryId == null)
                    break;

                parentCategory = await GetAsync(
                    c => c.Id == parentCategory.ParentCategoryId,
                    withSubCategories: false,
                    withRecipes: false,
                    isTracked: false,
                    cancellationToken: cancellationToken);
            }

            breadcrumbPath.Reverse();

            for (int i = 0; i < breadcrumbPath.Count; i++)
            {
                breadcrumbPath[i].Order = i + 1;
                await _dbBreadcrumb.CreateAsync(breadcrumbPath[i], cancellationToken);
            }
        }

        public async Task RemoveAsync(Category category, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(category);

            // Remove associated breadcrumbs
            var breadcrumbs = await _db.Breadcrumbs.Where(b => b.CategoryId == category.Id).ToListAsync(cancellationToken);
            if (breadcrumbs.Any())
            {
                _db.Breadcrumbs.RemoveRange(breadcrumbs);
                await SaveAsync(cancellationToken);
            }

            _db.Categories.Remove(category);
            await SaveAsync(cancellationToken);
        }

        public async Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(category);

            // Fetch original to check for parent change
            var original = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == category.Id, cancellationToken);
            if (original == null)
            {
                throw new InvalidOperationException("Category not found for update.");
            }

            bool parentChanged = original.ParentCategoryId != category.ParentCategoryId;

            _db.Categories.Update(category);
            await SaveAsync(cancellationToken);

            if (parentChanged)
            {
                // Remove old breadcrumbs
                var oldBreadcrumbs = await _db.Breadcrumbs.Where(b => b.CategoryId == category.Id).ToListAsync(cancellationToken);
                if (oldBreadcrumbs.Any())
                {
                    _db.Breadcrumbs.RemoveRange(oldBreadcrumbs);
                    await SaveAsync(cancellationToken);
                }

                // Recreate new breadcrumbs
                List<BreadcrumbItem> breadcrumbPath = new List<BreadcrumbItem>();

                var parentCategory = await GetAsync(
                    c => c.Id == category.ParentCategoryId,
                    withSubCategories: false,
                    withRecipes: false,
                    isTracked: false,
                    cancellationToken: cancellationToken);

                while (parentCategory != null)
                {
                    var breadcrumbItem = new BreadcrumbItem
                    {
                        Name = parentCategory.Name,
                        TransliteratedName = parentCategory.TransliteratedName,
                        CategoryId = category.Id
                    };
                    breadcrumbPath.Add(breadcrumbItem);

                    if (parentCategory.ParentCategoryId == null)
                        break;

                    parentCategory = await GetAsync(
                        c => c.Id == parentCategory.ParentCategoryId,
                        withSubCategories: false,
                        withRecipes: false,
                        isTracked: false,
                        cancellationToken: cancellationToken);
                }

                breadcrumbPath.Reverse();

                for (int i = 0; i < breadcrumbPath.Count; i++)
                {
                    breadcrumbPath[i].Order = i + 1;
                    await _dbBreadcrumb.CreateAsync(breadcrumbPath[i], cancellationToken);
                }
            }

            return category;
        }

        public Task SaveAsync(CancellationToken cancellationToken = default) =>
            _db.SaveChangesAsync(cancellationToken);
    }
}