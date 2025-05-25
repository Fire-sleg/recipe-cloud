using System.ComponentModel.DataAnnotations;

namespace RecommendationService.Models
{
    public class RecipeDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> Ingredients { get; set; }
        public int CookingTime { get; set; }
        public string Difficulty { get; set; }
        public string ImageUrl { get; set; } // MinIO URL
        public List<string> Diets { get; set; } = new List<string>();
        public List<string> Allergens { get; set; } = new List<string>();
        public string Cuisine { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public int Calories { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbohydrates { get; set; }
        public bool IsPremium { get; set; }
        public List<string> Directions { get; set; } = new List<string>();
        public int Serving { get; set; }



        public string TransliteratedName { get; set; } = string.Empty;
        public List<BreadcrumbItem>? BreadcrumbPath { get; set; }
        public Guid CategoryId { get; set; }

    }
}
