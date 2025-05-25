using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace RecommendationService.Models
{
    public class BreadcrumbItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
        public string? TransliteratedName { get; set; }
        public int Order { get; set; }
        public Guid? CategoryId { get; set; }

        [JsonIgnore]
        public Category Category { get; set; }

        public Guid? RecipeId { get; set; }
        [JsonIgnore]
        public Recipe? Recipe { get; set; }
    }
}
