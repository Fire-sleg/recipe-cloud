using System.ComponentModel.DataAnnotations;

namespace RecommendationService.Models
{
    public class ViewHistoryDTO
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid RecipeId { get; set; }
        public Guid CollectionId { get; set; }
        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    }
}
