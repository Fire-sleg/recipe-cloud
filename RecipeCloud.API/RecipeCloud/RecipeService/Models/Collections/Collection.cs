using Microsoft.EntityFrameworkCore;
using RecipeService.Models.Recipes;
using System.ComponentModel.DataAnnotations;

namespace RecipeService.Models.Collections
{
    public class Collection
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; }

        [Required]
        public List<Recipe> Recipes { get; set; }
        public Guid CreatedBy { get; set; } 

        [MaxLength(50)]
        public string CreatedByUsername { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Range(0, int.MaxValue)]
        public int TotalCalories { get; set; } 

        [Range(0, double.MaxValue)]
        public double TotalProtein { get; set; } 

        [Range(0, double.MaxValue)]
        public double TotalFat { get; set; } 

        [Range(0, double.MaxValue)]
        public double TotalCarbohydrates { get; set; }



    }
}
