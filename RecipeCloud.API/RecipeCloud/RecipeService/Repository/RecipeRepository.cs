using Microsoft.EntityFrameworkCore;
using Nest;
using Newtonsoft.Json;
using RecipeService.Data;
using RecipeService.Models.Breadcrumbs;
using RecipeService.Models.Filter;
using RecipeService.Models.Pagination;
using RecipeService.Models.Recipes;
using RecipeService.Models.Recipes.DTOs;
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

            query = query.Include(p => p.BreadcrumbPath);

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

            query = query.Include(p => p.BreadcrumbPath);

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

        public Expression<Func<Recipe, bool>> GetFilterExpression(RecipeFilterDTO filterDto)
        {
            return recipe =>
                (string.IsNullOrEmpty(filterDto.Title) || recipe.Title.Contains(filterDto.Title)) &&
                (filterDto.CategoryId == null || recipe.CategoryId == filterDto.CategoryId);
        }


        public async Task<bool> IncrementViewCountAsync(Guid recipeId)
        {
            var recipe = await _db.Recipes.FindAsync(recipeId);

            if (recipe == null)
            {
                return false;
            }

            recipe.ViewCount++;

            await SaveAsync();
            return true;
        }


        public async Task<List<Recipe>> FilterRecipesAsync(RecipeFilterDTO filterDto, PaginationParams? paginationParams = null, string? sortOrder = null)
        {
            IQueryable<Recipe> query = _db.Recipes
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filterDto.Title))
            {
                query = query.Where(p => p.Title.Contains(filterDto.Title));
            }

            if (filterDto.Diets != null && filterDto.Diets.Any())
            {
                foreach (var diet in filterDto.Diets)
                {
                    query = query.Where(r => r.Diets.Contains(diet));
                }
            }

            if (filterDto.Tags != null && filterDto.Tags.Any())
            {
                foreach (var tag in filterDto.Tags)
                {
                    query = query.Where(r => r.Tags.Contains(tag));
                }
            }

            if (filterDto.Allergens != null && filterDto.Allergens.Any())
            {
                foreach (var allergen in filterDto.Allergens)
                {
                    query = query.Where(r => !r.Allergens.Contains(allergen));
                }
            }

            if (filterDto.Cuisines != null && filterDto.Cuisines.Any())
            {
                foreach (var cuisine in filterDto.Cuisines)
                {
                    query = query.Where(r => r.Cuisine == cuisine);
                }
            }

            query = query.Where(r => r.CategoryId == filterDto.CategoryId);

            if (filterDto.IsUserCreated != null)
            {
                query = query.Where(r => r.IsUserCreated == filterDto.IsUserCreated);
            }

            switch (sortOrder)
            {
                case "CaloriesLowToHigh":
                    query = query.OrderBy(p => p.Calories);
                    break;
                case "CaloriesHighToLow":
                    query = query.OrderByDescending(p => p.Calories);
                    break;


                case "FatLowToHigh":
                    query = query.OrderBy(p => p.Fat);
                    break;
                case "FatHighToLow":
                    query = query.OrderByDescending(p => p.Fat);
                    break;


                case "CarbohydratesLowToHigh":
                    query = query.OrderBy(p => p.Carbohydrates);
                    break;
                case "CarbohydratesHighToLow":
                    query = query.OrderByDescending(p => p.Carbohydrates);
                    break;


                case "ProteinLowToHigh":
                    query = query.OrderBy(p => p.Protein);
                    break;
                case "ProteinHighToLow":
                    query = query.OrderByDescending(p => p.Protein);
                    break;


                case "CookingTimeLowToHigh":
                    query = query.OrderBy(p => p.CookingTime);
                    break;
                case "CookingTimeHighToLow":
                    query = query.OrderByDescending(p => p.CookingTime);
                    break;



                default:
                    query = query.OrderBy(p => p.Title);
                    break;
            }


            if (paginationParams != null)
            {
                query = query
                    .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                    .Take(paginationParams.PageSize);
            }

            return await query.ToListAsync();
        }
    }
}
