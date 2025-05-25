using RecipeService.Models.Breadcrumbs;
using RecipeService.Models.Recipes;

namespace RecipeService.Models.Categories.DTOs
{
    public class CategoryDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TransliteratedName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public List<BreadcrumbItem>? BreadcrumbPath { get; set; }
        public Guid? ParentCategoryId { get; set; }

        public virtual ICollection<Category>? SubCategories { get; set; }
        public virtual ICollection<Recipe>? Recipes { get; set; }

        public int Order { get; set; }
    }
}
