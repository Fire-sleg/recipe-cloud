using Microsoft.AspNetCore.Http;
using RecommendationService.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RecommendationService.Services
{
    public class ContentBasedRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _userServiceUrl;
        private readonly string _recipeServiceUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ContentBasedRecommendationService(HttpClient httpClient, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _userServiceUrl = config["ServiceUrls:UserService"];
            _recipeServiceUrl = config["ServiceUrls:RecipeService"];


            _httpContextAccessor = httpContextAccessor;

            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
            }
        }

        public async Task<List<RecipeDTO>> GetContentBasedRecommendations(Guid userId, UserPreferences preferences, int limit = 10)
        {
            // 1. Get All ViewHistory
            var viewHistory = await _httpClient.GetFromJsonAsync<List<ViewHistoryDTO>>(
                $"{_userServiceUrl}/api/view-history/10");

            var recommendations = new List<(RecipeDTO Recipe, double Score)>();

            // 2. For every viewed recipe
            foreach (var view in viewHistory.Where(v => v.RecipeId != Guid.Empty))
            {
                var apiResponse = await _httpClient.GetFromJsonAsync<APIResponse<RecipeDTO>>(
                    $"{_recipeServiceUrl}/api/recipes/{view.RecipeId}");

                var recipe = apiResponse?.Result;

                if (recipe == null) continue;

                // 3. Find similar recipes
                var similarRecipes = await _httpClient.GetFromJsonAsync<List<RecipeDTO>>(
                    $"{_recipeServiceUrl}/api/recipes/by-category/{recipe.CategoryId}");

                // 4. Filtering and evaluating similarity
                var filteredRecipes = FilterByPreferences(similarRecipes, preferences);
                if (filteredRecipes.Count < similarRecipes.Count/2)
                {
                    filteredRecipes = similarRecipes;
                }

                foreach (var similarRecipe in filteredRecipes)
                {
                    if (similarRecipe.Id == recipe.Id) continue;

                    var similarityScore = CalculateCosineSimilarity(recipe, similarRecipe);

                    recommendations.Add((similarRecipe, similarityScore));
                }

            }

            // 5. Sort and limit
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
                 preferences.DietaryPreferences.All(dp => r.Diets.Contains(dp))) &&

                (preferences.Allergens.Count == 0 ||
                 !preferences.Allergens.Any(a => !r.Allergens.Contains(a))) &&

                (preferences.FavoriteCuisines.Count == 0 ||
                 preferences.FavoriteCuisines.Contains(r.Cuisine)))
                .ToList();

        }

        private double CalculateCosineSimilarity(RecipeDTO a, RecipeDTO b)
        {
            var vectorA = new double[]
            {
                a.CookingTime,
                a.IsUserCreated ? 1.0 : 0.0,
                a.Protein,
                a.Fat,
                a.Carbohydrates
            };

            var vectorB = new double[]
            {
                b.CookingTime,
                b.IsUserCreated ? 1.0 : 0.0,
                b.Protein,
                b.Fat,
                b.Carbohydrates
            };

            double dotProduct = 0;
            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
            }

            double magnitudeA = Math.Sqrt(vectorA.Sum(x => x * x));
            double magnitudeB = Math.Sqrt(vectorB.Sum(x => x * x));

            return (magnitudeA == 0 || magnitudeB == 0) ? 0 : dotProduct / (magnitudeA * magnitudeB);
        }

    }


}
