using Microsoft.EntityFrameworkCore;
using RecipeService.Data;
using RecipeService.Models.Breadcrumbs;
using RecipeService.Models.Categories;
using System.Linq.Expressions;
using System.Linq;

namespace RecipeService.Repository
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IBreadcrumbRepository _dbBreadcrumb;
        public CategoryRepository(ApplicationDbContext db, IBreadcrumbRepository dbBreadcrumb)
        {
            _db = db;
            _dbBreadcrumb = dbBreadcrumb;
        }

        public async Task<List<Category>> GetAllAsync(Expression<Func<Category, bool>>? filter = null, bool withSubCategories = true,  bool withRecipes = true)
        {
            IQueryable<Category> query = _db.Categories;

            query = query.Include(c => c.BreadcrumbPath);

            if (withSubCategories)
            {
                query = query.Include(c => c.SubCategories);
            }
            if (withRecipes)
            {
                query = query.Include(c => c.Recipes.Where(p => p != null));

            }
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.ToListAsync();
        }

        public async Task<Category> GetAsync(Expression<Func<Category, bool>> filter = null, bool withSubCategories = true,  bool withRecipes = true, bool tracked = true)
        {
            IQueryable<Category> query = _db.Categories;

            query = query.Include(c => c.BreadcrumbPath);

            if (withSubCategories)
            {
                query = query.Include(c => c.SubCategories);
            }

            if (withRecipes)
            {
                query = query.Include(c => c.Recipes.Where(p => p != null));

            }
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
        public async Task CreateAsync(Category category)
        {
            category.TransliteratedName = Transliterator.Transliterate(category.Name);

            await _db.Categories.AddAsync(category);
            await SaveAsync();

            var createdCategory = await GetAsync(u => u.TransliteratedName == category.TransliteratedName);



            List<BreadcrumbItem> breadcrumbPath = new List<BreadcrumbItem>();

            var parentCategory = await GetAsync(
                c => c.Id == createdCategory.ParentCategoryId, 
                withSubCategories: false, 
                withRecipes: false, 
                tracked: false
            );


            if (parentCategory == null)
            {
                return;
            }

            var breadcrumbItem = new BreadcrumbItem()
            {
                Name = parentCategory.Name,
                TransliteratedName = parentCategory.TransliteratedName,
                CategoryId = createdCategory.Id,
            };
            breadcrumbPath.Add(breadcrumbItem);
            //await _dbBreadcrumb.CreateAsync(breadcrumbItem);

            while (parentCategory.ParentCategoryId != null)
            {
                parentCategory = await GetAsync(
                    c => c.Id == parentCategory.ParentCategoryId, 
                    withSubCategories: false,
                    withRecipes: false,
                    tracked: false
                );

                breadcrumbItem = new BreadcrumbItem()
                {
                    Name = parentCategory.Name,
                    TransliteratedName = parentCategory.TransliteratedName,
                    CategoryId = createdCategory.Id
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
            //category.BreadcrumbPath = breadcrumbPath;


        }

        public async Task RemoveAsync(Category category)
        {
            _db.Categories.Remove(category);
            await SaveAsync();
        }
        public async Task<Category> UpdateAsync(Category category)
        {
            //entity.UpdatedDate = DateTime.Now;
            _db.Categories.Update(category);
            await SaveAsync();
            return category;
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }


    }
}
