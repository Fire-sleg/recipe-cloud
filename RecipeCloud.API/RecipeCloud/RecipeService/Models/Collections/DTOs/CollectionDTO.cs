using RecipeService.Models.Recipes.DTOs;

namespace RecipeService.Models.Collections.DTOs
{
    public class CollectionDTO
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string CreatedByUsername { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int TotalCalories { get; set; }

        public double TotalProtein { get; set; }

        public double TotalFat { get; set; }

        public double TotalCarbohydrates { get; set; }

        public List<RecipeDTO> Recipes { get; set; } = new List<RecipeDTO>();
    }
}
