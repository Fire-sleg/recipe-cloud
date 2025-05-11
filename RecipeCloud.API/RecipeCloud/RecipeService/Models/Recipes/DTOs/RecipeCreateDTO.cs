using System.ComponentModel.DataAnnotations;

namespace RecipeService.Models.Recipes.DTOs
{
    public class RecipeCreateDTO
    {
        [Required, MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required, MinLength(1)]
        public List<string> Ingredients { get; set; }

        [Range(1, int.MaxValue)]
        public int CookingTime { get; set; }

        [Required, MaxLength(20)]
        public string Difficulty { get; set; }

        public List<string> Diets { get; set; } = new List<string>();

        public List<string> Allergens { get; set; } = new List<string>();

        [MaxLength(50)]
        public string Cuisine { get; set; }

        public List<string> Tags { get; set; } = new List<string>();

        [Range(0, int.MaxValue)]
        public int Calories { get; set; }

        [Range(0, double.MaxValue)]
        public double Protein { get; set; }

        [Range(0, double.MaxValue)]
        public double Fat { get; set; }

        [Range(0, double.MaxValue)]
        public double Carbohydrates { get; set; }

        public bool IsPremium { get; set; }
        public List<string> Directions { get; set; } = new List<string>();
    }
}
