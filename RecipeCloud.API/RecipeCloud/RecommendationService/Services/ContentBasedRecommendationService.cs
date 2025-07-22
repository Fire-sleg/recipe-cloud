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
            // 1. Отримати історію переглядів
            var viewHistory = await _httpClient.GetFromJsonAsync<List<ViewHistoryDTO>>(
                $"{_userServiceUrl}/api/view-history/10");

            var recommendations = new List<(RecipeDTO Recipe, double Score)>();

            // 2. Для кожного переглянутого рецепту
            foreach (var view in viewHistory.Where(v => v.RecipeId != Guid.Empty))
            {
                //var recipe = await _httpClient.GetFromJsonAsync<RecipeDTO>(
                //    $"{_recipeServiceUrl}/api/recipes/{view.RecipeId}");


                //var apiResponse = await _httpClient.GetFromJsonAsync<APIResponse>(
                //    $"{_recipeServiceUrl}/api/recipes/{view.RecipeId}");

                //var recipe = JsonSerializer.Deserialize<RecipeDTO>(
                //    apiResponse?.Result?.ToString() ?? string.Empty,
                //    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


                var apiResponse = await _httpClient.GetFromJsonAsync<APIResponse<RecipeDTO>>(
                    $"{_recipeServiceUrl}/api/recipes/{view.RecipeId}");

                var recipe = apiResponse?.Result;



                if (recipe == null) continue;

                // 3. Знайти схожі рецепти
                //var similarRecipes = await _httpClient.GetFromJsonAsync<List<RecipeDTO>>(
                //    $"{_recipeServiceUrl}/api/recipes?tags={string.Join(",", recipe.Tags)}");

                var similarRecipes = await _httpClient.GetFromJsonAsync<List<RecipeDTO>>(
                    $"{_recipeServiceUrl}/api/recipes/by-category/{recipe.CategoryId}");

                // 4. Фільтрувати та оцінити схожість
                var filteredRecipes = FilterByPreferences(similarRecipes, preferences);
                if (filteredRecipes.Count < similarRecipes.Count/2)
                {
                    filteredRecipes = similarRecipes;
                }

                //foreach (var similarRecipe in filteredRecipes)
                //{
                //    if (similarRecipe.Id == recipe.Id) continue;
                //    var similarityScore = CalculateCosineSimilarity(recipe.Tags, similarRecipe.Tags);
                //    recommendations.Add((similarRecipe, similarityScore));
                //}
                foreach (var similarRecipe in filteredRecipes)
                {
                    if (similarRecipe.Id == recipe.Id) continue;

                    var similarityScore = CalculateCosineSimilarity(recipe, similarRecipe);

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
            //return recipes.Where(r =>
            //    (preferences.DietaryPreferences.Count == 0 ||
            //     preferences.DietaryPreferences.All(dp => r.Tags.Contains(dp))) &&
            //    (preferences.Allergens.Count == 0 ||
            //     !preferences.Allergens.Any(a => r.Title.Contains(a, StringComparison.OrdinalIgnoreCase))))
            //    .ToList();

            //return recipes.Where(r =>
            //    (preferences.DietaryPreferences.Count == 0 ||
            //     preferences.DietaryPreferences.All(dp => r.Diets.Contains(dp))) &&
            //    (preferences.Allergens.Count == 0 ||
            //     !preferences.Allergens.Any(a => r.Allergens.Contains(a))) &&
            //      (preferences.FavoriteCuisines.Count == 0 ||
            //     !preferences.FavoriteCuisines.Any(c => c.Contains(r.Cuisine))))
            //    .ToList();

            return recipes.Where(r =>
                (preferences.DietaryPreferences.Count == 0 ||
                 preferences.DietaryPreferences.All(dp => r.Diets.Contains(dp))) &&

                (preferences.Allergens.Count == 0 ||
                 !preferences.Allergens.Any(a => !r.Allergens.Contains(a))) &&

                (preferences.FavoriteCuisines.Count == 0 ||
                 preferences.FavoriteCuisines.Contains(r.Cuisine)))
                .ToList();

        }

        //private double CalculateCosineSimilarity(List<string> tags1, List<string> tags2)
        //{
        //    var set1 = new HashSet<string>(tags1);
        //    var set2 = new HashSet<string>(tags2);
        //    var intersection = set1.Intersect(set2).Count();
        //    var magnitude = Math.Sqrt(set1.Count * set2.Count);
        //    return magnitude == 0 ? 0 : intersection / magnitude;
        //}
        private double CalculateCosineSimilarity(RecipeDTO a, RecipeDTO b)
        {
            // Перетворюємо об'єкти в числові вектори
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

            // Скалярний добуток
            double dotProduct = 0;
            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
            }

            // Модулі (довжини) векторів
            double magnitudeA = Math.Sqrt(vectorA.Sum(x => x * x));
            double magnitudeB = Math.Sqrt(vectorB.Sum(x => x * x));

            // Обчислюємо косинусну схожість
            return (magnitudeA == 0 || magnitudeB == 0) ? 0 : dotProduct / (magnitudeA * magnitudeB);
        }

    }


}
