using System.Text.Json;
using System;
using StackExchange.Redis;
using RecommendationService.Models;
using System.Net.Http.Headers;
using Microsoft.ML.Trainers;
using Microsoft.ML;

namespace RecommendationService.Services
{
    public class RecommendationMainService : IRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly IDatabase _redis;
        private readonly ContentBasedRecommendationService _contentBasedService;
        private readonly CollaborativeRecommendationService _collaborativeService;
        private readonly ContextualRecommendationService _contextualService;
        private readonly string _userServiceUrl;
        private readonly string _recipeServiceUrl;
        private readonly string _collectionServiceUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public RecommendationMainService(
            HttpClient httpClient,
            IConnectionMultiplexer redis,
            ContentBasedRecommendationService contentBasedService,
            CollaborativeRecommendationService collaborativeService,
            ContextualRecommendationService contextualService,
            IConfiguration config,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _redis = redis.GetDatabase();
            _contentBasedService = contentBasedService;
            _collaborativeService = collaborativeService;
            _contextualService = contextualService;
            _userServiceUrl = config["ServiceUrls:UserService"];
            _recipeServiceUrl = config["ServiceUrls:RecipeService"];
            _httpContextAccessor = httpContextAccessor;

            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
            }

        }

        public async Task<RecommendationResult> GetRecommendations(Guid userId, int limit = 6)
        {
            // 1. Перевірити кеш
            var cacheKey = $"recommendations:user:{userId}";
            var cached = await _redis.StringGetAsync(cacheKey);

            if (cached.HasValue)
                return new RecommendationResult { Recommendations = JsonSerializer.Deserialize<List<RecipeDTO>>(cached), Metrics = null };

            // 2. Отримати уподобання
            var preferences = await _httpClient.GetFromJsonAsync<UserPreferences>(
                $"{_userServiceUrl}/api/preferences/{userId}") ?? new UserPreferences { UserId = userId };

            // 3. Отримати рекомендації
            var contentBased = await _contentBasedService.GetContentBasedRecommendations(userId, preferences);
            var collaborative = await _collaborativeService.GetCollaborativeRecommendations(userId);

            // 4. Об’єднати та ранжувати
            var allItems = contentBased
                .Cast<RecipeDTO>()
                .Concat(collaborative.Cast<RecipeDTO>())
                .GroupBy(i => (i as dynamic).Id)
                .Select(g => g.First())
                .ToList();

            var rankedItems = RankItems(allItems, preferences)
                .Take(limit)
                .ToList();

            // 5. Кешувати та повернути
            var recommendationMetrics = CalculateRecommendationMetrics(rankedItems, userId, preferences);
            Dictionary<string, double> metrics = recommendationMetrics.Result;

            await _redis.StringSetAsync(cacheKey, JsonSerializer.Serialize(rankedItems), TimeSpan.FromHours(1)); //TimeSpan.FromHours(1)
            return new RecommendationResult() { Recommendations = rankedItems, Metrics = metrics };
        }

        private async Task<Dictionary<string, double>> CalculateRecommendationMetrics(
            List<RecipeDTO> recommendations, Guid userId, UserPreferences preferences)
        {
            var metrics = new Dictionary<string, double>();

            try
            {
                metrics["Diversity"] = CalculateIntraListDiversity(recommendations);
                metrics["Personalization"] = CalculatePersonalization(recommendations, preferences);
                metrics["ContentRelevance"] = CalculateContentRelevance(recommendations, preferences);

                return metrics;
            }
            catch
            {
                return new Dictionary<string, double> { ["Error"] = 1.0 };
            }
        }

        private double CalculateIntraListDiversity(List<RecipeDTO> recommendations)
        {
            if (recommendations.Count < 2) return 0;

            double totalDistance = 0;
            int pairCount = 0;

            for (int i = 0; i < recommendations.Count; i++)
            {
                for (int j = i + 1; j < recommendations.Count; j++)
                {
                    totalDistance += CalculateRecipeDistance(recommendations[i], recommendations[j]);
                    pairCount++;
                }
            }

            return pairCount > 0 ? totalDistance / pairCount : 0;
        }

        private double CalculateRecipeDistance(RecipeDTO recipe1, RecipeDTO recipe2)
        {
            double distance = 0;

            // Оновлені ваги для відстані
            if (recipe1.CategoryId != recipe2.CategoryId) distance += 0.25;
            if (recipe1.Cuisine != recipe2.Cuisine) distance += 0.25;

            var timeDiff = Math.Abs(recipe1.CookingTime - recipe2.CookingTime);
            distance += Math.Min(timeDiff / 60.0, 1.0) * 0.15;

            var ingredients1 = recipe1.Ingredients?.ToHashSet() ?? new HashSet<string>();
            var ingredients2 = recipe2.Ingredients?.ToHashSet() ?? new HashSet<string>();
            if (ingredients1.Any() || ingredients2.Any())
            {
                var intersection = ingredients1.Intersect(ingredients2).Count();
                var union = ingredients1.Union(ingredients2).Count();
                distance += (union > 0 ? 1.0 - (double)intersection / union : 1.0) * 0.35;
            }

            return Math.Min(distance, 1.0);
        }
        
        private double CalculatePersonalization(List<RecipeDTO> recommendations, UserPreferences preferences)
        {
            if (!recommendations.Any()) return 0;

            double totalScore = recommendations.Average(r => CalculateRecipeRelevanceScore(r, preferences));
            return Normalize(totalScore, 0, 1);
        }

        private double CalculateContentRelevance(List<RecipeDTO> recommendations, UserPreferences preferences)
        {
            if (!recommendations.Any()) return 0;

            return recommendations.Average(r => CalculateRecipeRelevanceScore(r, preferences));
        }

        private double CalculateRecipeRelevanceScore(RecipeDTO recipe, UserPreferences preferences)
        {
            double score = 0;
            double maxScore = 1.0;

            // Оновлені ваги для релевантності
            if (preferences.FavoriteCuisines?.Contains(recipe.Cuisine) == true)
                score += 0.5;

            if (preferences.DietaryPreferences?.Any() == true)
            {
                var satisfiedRestrictions = preferences.DietaryPreferences
                    .Count(restriction => recipe.Diets?.Contains(restriction) == true);
                score += (double)satisfiedRestrictions / preferences.DietaryPreferences.Count * 0.5;
            }
            else
            {
                score += 0.5;
            }

            return score / maxScore;
        }
        private async Task<List<RecipeRating>> GetUserRatings(Guid userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<RecipeRating>>(
                    $"{_recipeServiceUrl}/api/rating/get-user-ratings") ?? new List<RecipeRating>();
            }
            catch
            {
                return new List<RecipeRating>();
            }
        }

        private List<RecipeDTO> RankItems(List<RecipeDTO> items, UserPreferences preferences)
        {
            var ranked = new List<(RecipeDTO Item, double Score)>();
            var userTags = preferences.DietaryPreferences.Concat(preferences.FavoriteCuisines).ToList();

            foreach (var item in items)
            {
                double score = 0;
                var tags = new List<string>() { item.Cuisine };
                tags.AddRange(item.Diets);
                var viewCount = (int)(item as dynamic).ViewCount;
                var averageRating = (item as dynamic).AverageRating as double? ?? 0;

                score += CalculateCosineSimilarity(userTags, tags) * RecommendationConfig.ContentBasedWeight;
                score += Normalize(averageRating) * RecommendationConfig.CollaborativeWeight;
                score += Normalize(viewCount) * RecommendationConfig.PopularityWeight;

                ranked.Add((item, score));
            }

            return ranked.OrderByDescending(x => x.Score).Select(x => x.Item).ToList();
        }

        private double CalculateCosineSimilarity(List<string> tags1, List<string> tags2)
        {
            var set1 = new HashSet<string>(tags1);
            var set2 = new HashSet<string>(tags2);
            var intersection = set1.Intersect(set2).Count();
            var magnitude = Math.Sqrt(set1.Count * set2.Count);
            return magnitude == 0 ? 0 : intersection / magnitude;
        }

        private double Normalize(double value, double min = 0, double max = 1000)
        {
            return Math.Max(0, Math.Min(1, (value - min) / (max - min)));
        }
    }

}
