using System.Text.Json;
using System;
using StackExchange.Redis;
using RecommendationService.Models;

namespace RecommendationService.Services
{
    public class RecommendationMainService
    {
        private readonly HttpClient _httpClient;
        private readonly IDatabase _redis;
        private readonly ContentBasedRecommendationService _contentBasedService;
        private readonly CollaborativeRecommendationService _collaborativeService;
        private readonly ContextualRecommendationService _contextualService;
        private readonly string _userServiceUrl;
        private readonly string _recipeServiceUrl;
        private readonly string _collectionServiceUrl;

        public RecommendationMainService(
            HttpClient httpClient,
            IConnectionMultiplexer redis,
            ContentBasedRecommendationService contentBasedService,
            CollaborativeRecommendationService collaborativeService,
            ContextualRecommendationService contextualService,
            IConfiguration config)
        {
            _httpClient = httpClient;
            _redis = redis.GetDatabase();
            _contentBasedService = contentBasedService;
            _collaborativeService = collaborativeService;
            _contextualService = contextualService;
            _userServiceUrl = config["ServiceUrls:UserService"];
            _recipeServiceUrl = config["ServiceUrls:RecipeService"];
            _collectionServiceUrl = config["ServiceUrls:CollectionService"];
        }

        public async Task<List<object>> GetRecommendations(Guid userId, int limit = 6)
        {
            // 1. Перевірити кеш
            var cacheKey = $"recommendations:user:{userId}";
            var cached = await _redis.StringGetAsync(cacheKey);
            if (cached.HasValue)
                return JsonSerializer.Deserialize<List<object>>(cached);

            // 2. Отримати уподобання
            var preferences = await _httpClient.GetFromJsonAsync<UserPreferences>(
                $"{_userServiceUrl}/api/users/{userId}/preferences") ?? new UserPreferences { UserId = userId };

            // 3. Отримати рекомендації
            var contentBased = await _contentBasedService.GetContentBasedRecommendations(userId, preferences);
            var collaborative = await _collaborativeService.GetCollaborativeRecommendations(userId);
            var contextual = await _contextualService.GetContextualRecommendations(preferences);
            var popularRecipes = await _httpClient.GetFromJsonAsync<List<RecipeDTO>>(
                $"{_recipeServiceUrl}/api/recipes?sort=popular&limit={limit / 2}");
            var popularCollections = await _httpClient.GetFromJsonAsync<List<CollectionDTO>>(
                $"{_collectionServiceUrl}/api/collections?sort=popular&limit={limit / 2}");

            // 4. Об’єднати та ранжувати
            var allItems = contentBased
                .Cast<object>()
                .Concat(collaborative.Cast<object>())
                .Concat(contextual.Cast<object>())
                .Concat(popularRecipes.Cast<object>())
                .Concat(popularCollections.Cast<object>())
                .GroupBy(i => (i as dynamic).Id)
                .Select(g => g.First())
                .ToList();

            var rankedItems = RankItems(allItems, preferences)
                .Take(limit)
                .ToList();

            // 5. Кешувати та повернути
            await _redis.StringSetAsync(cacheKey, JsonSerializer.Serialize(rankedItems), TimeSpan.FromHours(1));
            return rankedItems;
        }

        private List<object> RankItems(List<object> items, UserPreferences preferences)
        {
            var ranked = new List<(object Item, double Score)>();
            var now = DateTime.UtcNow;
            var isMorning = now.Hour >= 6 && now.Hour < 12;
            var userTags = preferences.DietaryPreferences.Concat(preferences.FavoriteCuisines).ToList();

            foreach (var item in items)
            {
                double score = 0;
                var tags = (item as dynamic).Tags as List<string>;
                var categoryName = (item as dynamic).CategoryName as string;
                var viewCount = (int)(item as dynamic).ViewCount;
                var averageRating = (item as dynamic).AverageRating as double? ?? 0;

                score += CalculateCosineSimilarity(userTags, tags) * RecommendationConfig.ContentBasedWeight;
                score += Normalize(averageRating) * RecommendationConfig.CollaborativeWeight;
                if (isMorning && categoryName == "Breakfast") score += RecommendationConfig.ContextualWeight;
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
