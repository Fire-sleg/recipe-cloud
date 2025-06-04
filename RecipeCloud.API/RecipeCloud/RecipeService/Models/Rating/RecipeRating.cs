using System.ComponentModel.DataAnnotations;

namespace RecipeService.Models.Rating
{
    public class RecipeRating
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid RecipeId { get; set; }
        public int Rating { get; set; }
        public DateTime RatedAt { get; set; }
    }
}
