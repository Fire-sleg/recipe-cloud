using Microsoft.EntityFrameworkCore;
using RecipeService.Data;
using RecipeService.Models.Breadcrumbs;
using RecipeService.Models.Filter;
using RecipeService.Models.Pagination;
using RecipeService.Models.Recipes;
using System.Linq.Expressions;
using System.Threading;

namespace RecipeService.Repository;

public class RecipeRepository : IRecipeRepository
{
    private readonly ApplicationDbContext _db;
    private readonly ICategoryRepository _dbCategory;
    private readonly IBreadcrumbRepository _dbBreadcrumb;

    public RecipeRepository(
        ApplicationDbContext db,
        ICategoryRepository dbCategory,
        IBreadcrumbRepository dbBreadcrumb)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(dbCategory);
        ArgumentNullException.ThrowIfNull(dbBreadcrumb);

        _db = db;
        _dbCategory = dbCategory;
        _dbBreadcrumb = dbBreadcrumb;
    }

    public async Task CreateAsync(Recipe entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.TransliteratedName = Transliterator.Transliterate(entity.Title);

        await _db.Recipes.AddAsync(entity, cancellationToken);
        await SaveAsync(cancellationToken);

        var breadcrumbPath = new List<BreadcrumbItem>();

        var parentCategory = await _dbCategory.GetAsync(
            c => c.Id == entity.CategoryId,
            withSubCategories: false,
            withRecipes: false,
            tracked: false,
            cancellationToken: cancellationToken);

        while (parentCategory is not null)
        {
            breadcrumbPath.Add(new BreadcrumbItem
            {
                Name = parentCategory.Name,
                TransliteratedName = parentCategory.TransliteratedName,
                RecipeId = entity.Id
            });

            if (parentCategory.ParentCategoryId is null)
                break;

            parentCategory = await _dbCategory.GetAsync(
                c => c.Id == parentCategory.ParentCategoryId,
                withSubCategories: false,
                withRecipes: false,
                tracked: false,
                cancellationToken: cancellationToken);
        }

        breadcrumbPath.Reverse();

        for (var i = 0; i < breadcrumbPath.Count; i++)
        {
            breadcrumbPath[i].Order = i + 1;
            await _dbBreadcrumb.CreateAsync(breadcrumbPath[i], cancellationToken);
        }
    }

    public async Task<Recipe> UpdateAsync(Recipe entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var original = await _db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == entity.Id, cancellationToken);
        if (original == null)
        {
            throw new InvalidOperationException("Recipe not found for update.");
        }

        bool categoryChanged = original.CategoryId != entity.CategoryId;

        _db.Recipes.Update(entity);
        await SaveAsync(cancellationToken);

        if (categoryChanged)
        {
            // Remove old breadcrumbs
            var oldBreadcrumbs = await _db.Breadcrumbs.Where(b => b.RecipeId == entity.Id).ToListAsync(cancellationToken);
            if (oldBreadcrumbs.Any())
            {
                _db.Breadcrumbs.RemoveRange(oldBreadcrumbs);
                await SaveAsync(cancellationToken);
            }

            // Recreate new breadcrumbs
            var breadcrumbPath = new List<BreadcrumbItem>();

            var parentCategory = await _dbCategory.GetAsync(
                c => c.Id == entity.CategoryId,
                withSubCategories: false,
                withRecipes: false,
                tracked: false,
                cancellationToken: cancellationToken);

            while (parentCategory is not null)
            {
                breadcrumbPath.Add(new BreadcrumbItem
                {
                    Name = parentCategory.Name,
                    TransliteratedName = parentCategory.TransliteratedName,
                    RecipeId = entity.Id
                });

                if (parentCategory.ParentCategoryId is null)
                    break;

                parentCategory = await _dbCategory.GetAsync(
                    c => c.Id == parentCategory.ParentCategoryId,
                    withSubCategories: false,
                    withRecipes: false,
                    tracked: false,
                    cancellationToken: cancellationToken);
            }

            breadcrumbPath.Reverse();

            for (var i = 0; i < breadcrumbPath.Count; i++)
            {
                breadcrumbPath[i].Order = i + 1;
                await _dbBreadcrumb.CreateAsync(breadcrumbPath[i], cancellationToken);
            }
        }

        return entity;
    }

    public async Task RemoveAsync(Recipe entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Remove associated breadcrumbs first
        var breadcrumbs = await _db.Breadcrumbs.Where(b => b.RecipeId == entity.Id).ToListAsync(cancellationToken);
        if (breadcrumbs.Any())
        {
            _db.Breadcrumbs.RemoveRange(breadcrumbs);
            await SaveAsync(cancellationToken);
        }

        _db.Recipes.Remove(entity);
        await SaveAsync(cancellationToken);
    }

    public async Task<List<Recipe>> GetAllAsync(
        Expression<Func<Recipe, bool>>? filter = null,
        PaginationParams? paginationParams = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Recipe> query = _db.Recipes
            .Include(p => p.Category)
            .Include(p => p.BreadcrumbPath)
            .AsSplitQuery();

        if (filter is not null)
            query = query.Where(filter);

        query = ApplyPagination(query, paginationParams);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Recipe?> GetAsync(
        Expression<Func<Recipe, bool>>? filter = null,
        bool isTracked = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Recipe> query = _db.Recipes
            .Include(p => p.Category)
            .Include(p => p.BreadcrumbPath)
            .AsSplitQuery();

        if (!isTracked)
            query = query.AsNoTracking();

        if (filter is not null)
            query = query.Where(filter);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<int> CountAsync(
        Expression<Func<Recipe, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Recipe> query = _db.Recipes;

        if (filter is not null)
            query = query.Where(filter);

        return await query.CountAsync(cancellationToken);
    }

    public Expression<Func<Recipe, bool>> GetFilterExpression(RecipeFilterDTO filterDto)
    {
        ArgumentNullException.ThrowIfNull(filterDto);

        return recipe =>
            (string.IsNullOrEmpty(filterDto.Title) || recipe.Title.Contains(filterDto.Title)) &&

            (filterDto.CategoryId == null || recipe.CategoryId == filterDto.CategoryId) &&

            (!filterDto.Diets.Any() || recipe.Diets.Any(d => filterDto.Diets.Contains(d))) &&

            (!filterDto.Allergens.Any() || recipe.Allergens.Any(a => filterDto.Allergens.Contains(a))) &&

            (!filterDto.Tags.Any() || recipe.Tags.Any(t => filterDto.Tags.Contains(t))) &&

            (!filterDto.Cuisines.Any() || filterDto.Cuisines.Contains(recipe.Cuisine)) &&

            (filterDto.IsUserCreated == null || recipe.IsUserCreated == filterDto.IsUserCreated);
    }


    public async Task<(List<Recipe> Recipes, int TotalCount)> FilterWithCountAsync(
        Expression<Func<Recipe, bool>> filter,
        PaginationParams paginationParams,
        string? sortOrder,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Recipe> query = _db.Recipes.Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);

        query = sortOrder switch
        {
            "title_asc" => query.OrderBy(r => r.Title),
            "title_desc" => query.OrderByDescending(r => r.Title),
            "date_asc" => query.OrderBy(r => r.CreatedAt),
            "date_desc" => query.OrderByDescending(r => r.CreatedAt),
            _ => query.OrderBy(r => r.Id)
        };

        var recipes = await query
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync(cancellationToken);

        return (recipes, totalCount);
    }

    public async Task<bool> IncrementViewCountAsync(Guid recipeId, CancellationToken cancellationToken = default)
    {
        var recipe = await _db.Recipes.FindAsync([recipeId], cancellationToken);
        if (recipe is null) return false;

        recipe.ViewCount++;
        await SaveAsync(cancellationToken);
        return true;
    }

    public async Task<List<Recipe>> FilterRecipesAsync(
        RecipeFilterDTO filterDto,
        PaginationParams? paginationParams = null,
        string? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterDto);

        IQueryable<Recipe> query = _db.Recipes
            .Include(p => p.Category)
            .Include(p => p.BreadcrumbPath)
            .AsSplitQuery()
            .AsQueryable();

        query = ApplyFilters(query, filterDto);
        query = ApplySorting(query, sortOrder);
        query = ApplyPagination(query, paginationParams);

        return await query.ToListAsync(cancellationToken);
    }

    private IQueryable<Recipe> ApplyFilters(IQueryable<Recipe> query, RecipeFilterDTO filterDto)
    {
        if (!string.IsNullOrWhiteSpace(filterDto.Title))
        {
            query = query.Where(r => r.Title.Contains(filterDto.Title));
        }

        if (filterDto.Diets?.Count > 0)
        {
            query = query.Where(r => filterDto.Diets.All(diet => r.Diets.Contains(diet)));
        }

        if (filterDto.Tags?.Count > 0)
        {
            query = query.Where(r => filterDto.Tags.All(tag => r.Tags.Contains(tag)));
        }

        if (filterDto.Allergens?.Count > 0)
        {
            query = query.Where(r => !filterDto.Allergens.Any(allergen => r.Allergens.Contains(allergen)));
        }

        if (filterDto.Cuisines?.Count > 0)
        {
            query = query.Where(r => filterDto.Cuisines.Contains(r.Cuisine));
        }

        if (filterDto.CategoryId is not null)
        {
            query = query.Where(r => r.CategoryId == filterDto.CategoryId);
        }

        if (filterDto.IsUserCreated is not null)
        {
            query = query.Where(r => r.IsUserCreated == filterDto.IsUserCreated);
        }

        return query;
    }

    private static IQueryable<Recipe> ApplyPagination(IQueryable<Recipe> query, PaginationParams? paginationParams)
    {
        if (paginationParams is null) return query;

        int pageNumber = Math.Max(paginationParams.PageNumber, 1);  // Ensure positive
        int pageSize = Math.Max(paginationParams.PageSize, 1);

        return query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }

    private static IQueryable<Recipe> ApplySorting(IQueryable<Recipe> query, string? sortOrder) =>
        sortOrder switch
        {
            "CaloriesLowToHigh" => query.OrderBy(p => p.Calories),
            "CaloriesHighToLow" => query.OrderByDescending(p => p.Calories),
            "FatLowToHigh" => query.OrderBy(p => p.Fat),
            "FatHighToLow" => query.OrderByDescending(p => p.Fat),
            "CarbohydratesLowToHigh" => query.OrderBy(p => p.Carbohydrates),
            "CarbohydratesHighToLow" => query.OrderByDescending(p => p.Carbohydrates),
            "ProteinLowToHigh" => query.OrderBy(p => p.Protein),
            "ProteinHighToLow" => query.OrderByDescending(p => p.Protein),
            "CookingTimeLowToHigh" => query.OrderBy(p => p.CookingTime),
            "CookingTimeHighToLow" => query.OrderByDescending(p => p.CookingTime),
            _ => query.OrderBy(p => p.Title)
        };
}
