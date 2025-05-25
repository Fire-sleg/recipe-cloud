namespace RecommendationService.Models
{
    public class UserPreferences
    {
        public Guid UserId { get; set; }
        public List<string> DietaryPreferences { get; set; } = new List<string>();
        public List<string> Allergens { get; set; } = new List<string>();
        public List<string> FavoriteCuisines { get; set; } = new List<string>();

    }
}
