using System.ComponentModel.DataAnnotations;

namespace RecipeService.Models.Collections.DTOs
{
    public class CollectionUpdateDTO
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(50)]
        public string CreatedByUsername { get; set; }

        public List<Guid> RecipeIds { get; set; } = new List<Guid>();
    }
}
