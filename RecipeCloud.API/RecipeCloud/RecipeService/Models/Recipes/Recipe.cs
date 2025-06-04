using Newtonsoft.Json;
using RecipeService.Models.Breadcrumbs;
using RecipeService.Models.Categories;
using System.ComponentModel.DataAnnotations;

namespace RecipeService.Models.Recipes
{
    public class Recipe
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        public List<string> Ingredients { get; set; } = new List<string>();

        [Range(1, int.MaxValue)]
        public int CookingTime { get; set; } //minutes

        [Required, MaxLength(20)]
        public string Difficulty { get; set; } // easy, medium, hard

        public string ImageUrl { get; set; } // MinIO URL

        public Guid CreatedBy { get; set; } // User.Id

        [MaxLength(50)]
        public string CreatedByUsername { get; set; } // Для відображення автора
        public bool IsUserCreated { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<string> Diets { get; set; } = new List<string>();

        public List<string> Allergens { get; set; } = new List<string>();

        [MaxLength(50)]
        public string Cuisine { get; set; }

        public List<string> Tags { get; set; } = new List<string>();

        [Range(0, int.MaxValue)]
        public int Calories { get; set; } // ккал

        [Range(0, double.MaxValue)]
        public double Protein { get; set; } // г

        [Range(0, double.MaxValue)]
        public double Fat { get; set; } // г

        [Range(0, double.MaxValue)]
        public double Carbohydrates { get; set; } // г

        public bool IsPremium { get; set; }

        public List<string> Directions { get; set; } = new List<string>();
        public int Serving { get; set; }




        public string TransliteratedName { get; set; } = string.Empty;
        public List<BreadcrumbItem>? BreadcrumbPath { get; set; }
        public Guid CategoryId { get; set; }

        [JsonIgnore]
        public virtual Category? Category { get; set; }

        public int ViewCount { get; set; }
        public double AverageRating { get; set; }
    }
}
