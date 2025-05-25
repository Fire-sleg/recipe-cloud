using RecommendationService.Models;

namespace RecommendationService.Services
{
    public class ContextualRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _recipeServiceUrl;

        public ContextualRecommendationService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _recipeServiceUrl = config["ServiceUrls:RecipeService"];
        }

        public async Task<List<RecipeDTO>> GetContextualRecommendations(UserPreferences preferences, int limit = 5)
        {
            var now = DateTime.UtcNow;
            var isMorning = now.Hour >= 6 && now.Hour < 12;
            var season = GetSeason(now);

            var query = isMorning ? "category=Breakfast" : $"tags={season.ToLower()}";
            var recipes = await _httpClient.GetFromJsonAsync<List<RecipeDTO>>(
                $"{_recipeServiceUrl}/api/recipes?{query}");

            return FilterByPreferences(recipes, preferences)
                .Take(limit)
                .ToList();
        }

        private string GetSeason(DateTime date)
        {
            var month = date.Month;
            if (month >= 12 || month <= 2) return "Winter";
            if (month >= 3 && month <= 5) return "Spring";
            if (month >= 6 && month <= 8) return "Summer";
            return "Autumn";
        }

        private List<RecipeDTO> FilterByPreferences(List<RecipeDTO> recipes, UserPreferences preferences)
        {
            return recipes.Where(r =>
                (preferences.DietaryPreferences.Count == 0 ||
                 preferences.DietaryPreferences.All(dp => r.Tags.Contains(dp))) &&
                (preferences.Allergens.Count == 0 ||
                 !preferences.Allergens.Any(a => r.Title.Contains(a, StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }
    }
}
