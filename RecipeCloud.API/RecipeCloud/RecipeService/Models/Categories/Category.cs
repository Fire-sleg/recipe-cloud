using RecipeService.Models.Breadcrumbs;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Newtonsoft.Json;
using RecipeService.Models.Recipes;

namespace RecipeService.Models.Categories
{
    public class Category
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string TransliteratedName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public List<BreadcrumbItem>? BreadcrumbPath { get; set; }
        public Guid? ParentCategoryId { get; set; }

        [JsonIgnore]
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category>? SubCategories { get; set; }
        public virtual ICollection<Recipe>? Recipes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int Order { get; set; }
    }
}
