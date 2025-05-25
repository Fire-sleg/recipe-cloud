using RecommendationService.Models;

namespace RecommendationService.Services
{
    public class ContentBasedRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _userServiceUrl;
        private readonly string _recipeServiceUrl;

        public ContentBasedRecommendationService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _userServiceUrl = config["ServiceUrls:UserService"];
            _recipeServiceUrl = config["ServiceUrls:RecipeService"];
        }

        public async Task<List<RecipeDTO>> GetContentBasedRecommendations(Guid userId, UserPreferences preferences, int limit = 10)
        {
            // 1. Отримати історію переглядів
            var viewHistory = await _httpClient.GetFromJsonAsync<List<ViewHistoryDTO>>(
                $"{_userServiceUrl}/api/users/{userId}/view-history?limit=10");

            var recommendations = new List<(RecipeDTO Recipe, double Score)>();

            // 2. Для кожного переглянутого рецепту
            foreach (var view in viewHistory.Where(v => v.RecipeId != Guid.Empty))
            {
                var recipe = await _httpClient.GetFromJsonAsync<RecipeDTO>(
                    $"{_recipeServiceUrl}/api/recipes/{view.RecipeId}");
                if (recipe == null) continue;

                // 3. Знайти схожі рецепти
                var similarRecipes = await _httpClient.GetFromJsonAsync<List<RecipeDTO>>(
                    $"{_recipeServiceUrl}/api/recipes?tags={string.Join(",", recipe.Tags)}");

                // 4. Фільтрувати та оцінити схожість
                var filteredRecipes = FilterByPreferences(similarRecipes, preferences);
                foreach (var similarRecipe in filteredRecipes)
                {
                    if (similarRecipe.Id == recipe.Id) continue;
                    var similarityScore = CalculateCosineSimilarity(recipe.Tags, similarRecipe.Tags);
                    recommendations.Add((similarRecipe, similarityScore));
                }
            }

            // 5. Сортувати та обмежити
            return recommendations
                .OrderByDescending(r => r.Score)
                .Take(limit)
                .Select(r => r.Recipe)
                .ToList();
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

        private double CalculateCosineSimilarity(List<string> tags1, List<string> tags2)
        {
            var set1 = new HashSet<string>(tags1);
            var set2 = new HashSet<string>(tags2);
            var intersection = set1.Intersect(set2).Count();
            var magnitude = Math.Sqrt(set1.Count * set2.Count);
            return magnitude == 0 ? 0 : intersection / magnitude;
        }
    }
}
