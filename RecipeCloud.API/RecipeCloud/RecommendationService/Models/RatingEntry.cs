using System.ComponentModel.DataAnnotations;

namespace RecommendationService.Models
{
    public class RatingEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid ItemId { get; set; } // RecipeId або CollectionId

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public float Rating { get; set; } // Оцінка від 1 до 5

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string ItemType { get; set; } // "Recipe" або "Collection"
    }
}
