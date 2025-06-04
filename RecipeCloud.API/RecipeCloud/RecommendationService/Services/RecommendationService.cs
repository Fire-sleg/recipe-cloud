using System.Text.Json;
using System;
using StackExchange.Redis;
using RecommendationService.Models;
using System.Net.Http.Headers;

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
            _collectionServiceUrl = config["ServiceUrls:CollectionService"];
            _httpContextAccessor = httpContextAccessor;

            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
            }

        }

        public async Task<List<RecipeDTO>> GetRecommendations(Guid userId, int limit = 6)
        {
            //_userServiceUrl = "https://localhost:5050"; 
            //_recipeServiceUrl = "https://localhost:5051";


            // 1. Перевірити кеш
            var cacheKey = $"recommendations:user:{userId}";
            var cached = await _redis.StringGetAsync(cacheKey);



            if (cached.HasValue)
                return JsonSerializer.Deserialize<List<RecipeDTO>>(cached);

            // 2. Отримати уподобання
            Console.WriteLine($"{_userServiceUrl}/api/preferences/{userId}");


            var preferences = await _httpClient.GetFromJsonAsync<UserPreferences>(
                $"{_userServiceUrl}/api/preferences/{userId}") ?? new UserPreferences { UserId = userId };
            //_userServiceUrl
            // 3. Отримати рекомендації
            var contentBased = await _contentBasedService.GetContentBasedRecommendations(userId, preferences);
            var collaborative = await _collaborativeService.GetCollaborativeRecommendations(userId);



            //var contextual = await _contextualService.GetContextualRecommendations(preferences);



            //var popularRecipes = await _httpClient.GetFromJsonAsync<List<RecipeDTO>>(
            //    $"{_recipeServiceUrl}/api/recipes?sort=popular&limit={limit / 2}");
            //var popularCollections = await _httpClient.GetFromJsonAsync<List<CollectionDTO>>(
            //    $"{_collectionServiceUrl}/api/collections?sort=popular&limit={limit / 2}");

            // 4. Об’єднати та ранжувати
            var allItems = contentBased
                .Cast<RecipeDTO>()
                .Concat(collaborative.Cast<RecipeDTO>())


                //.Concat(contextual.Cast<object>())


                //.Concat(popularRecipes.Cast<object>())
                //.Concat(popularCollections.Cast<object>())
                .GroupBy(i => (i as dynamic).Id)
                .Select(g => g.First())
                .ToList();

            var rankedItems = RankItems(allItems, preferences)
                .Take(limit)
                .ToList();

            if (rankedItems.Count < limit)
            {
                var list = new List<RecipeDTO>();
                var seenRecipeIds = new HashSet<Guid>();

                var remaining = limit - rankedItems.Count;

                var viewHistory = await _httpClient.GetFromJsonAsync<List<ViewHistoryDTO>>(
                    $"{_userServiceUrl}/api/view-history/{remaining * 2}"); // запитуємо більше, бо можуть бути дублікати

                foreach (var view in viewHistory.Where(v => v.RecipeId != Guid.Empty))
                {
                    if (list.Count >= remaining) break;
                    if (seenRecipeIds.Contains(view.RecipeId)) continue;

                    var apiResponse = await _httpClient.GetFromJsonAsync<APIResponse<RecipeDTO>>(
                        $"{_recipeServiceUrl}/api/recipes/{view.RecipeId}");

                    var recipe = apiResponse?.Result;

                    if (recipe != null && !seenRecipeIds.Contains(recipe.Id))
                    {
                        seenRecipeIds.Add(recipe.Id);
                        list.Add(recipe);
                    }
                }

                rankedItems.AddRange(list);
            }


            // 5. Кешувати та повернути
            await _redis.StringSetAsync(cacheKey, JsonSerializer.Serialize(rankedItems), TimeSpan.FromHours(1));
            return rankedItems;
        }

        private List<RecipeDTO> RankItems(List<RecipeDTO> items, UserPreferences preferences)
        {
            var ranked = new List<(RecipeDTO Item, double Score)>();
            var now = DateTime.UtcNow;
            var isMorning = now.Hour >= 6 && now.Hour < 12;
            var userTags = preferences.DietaryPreferences.Concat(preferences.FavoriteCuisines).ToList();

            foreach (var item in items)
            {
                double score = 0;
                //var tags = (item as dynamic).Tags as List<string>;
                //var categoryName = (item as dynamic).CategoryName as string;
                var viewCount = (int)(item as dynamic).ViewCount;
                var averageRating = (item as dynamic).AverageRating as double? ?? 0;

                //score += CalculateCosineSimilarity(userTags, tags) * RecommendationConfig.ContentBasedWeight;
                score += Normalize(averageRating) * RecommendationConfig.CollaborativeWeight;
                //if (isMorning && categoryName == "Breakfast") score += RecommendationConfig.ContextualWeight;
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
