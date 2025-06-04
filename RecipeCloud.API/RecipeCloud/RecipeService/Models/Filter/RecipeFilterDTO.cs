using System.ComponentModel.DataAnnotations;

namespace RecipeService.Models.Filter
{
    public class RecipeFilterDTO
    {
        public string? Title { get; set; }
        public int? CookingTime { get; set; }
        public string? CreatedByUsername { get; set; }
        public bool? IsUserCreated { get; set; }
        public List<string>? Diets { get; set; } = new List<string>();
        public List<string>? Allergens { get; set; } = new List<string>();
        public List<string>? Cuisines { get; set; } = new List<string>();
        public List<string>? Tags { get; set; } = new List<string>();
        public int? Calories { get; set; } 
        public double? Protein { get; set; } 
        public double? Fat { get; set; } 
        public double? Carbohydrates { get; set; } 
        public Guid? CategoryId { get; set; }
    }
}
