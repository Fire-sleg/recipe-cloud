using Microsoft.ML;
using Microsoft.ML.Trainers;
using RecommendationService.Models;
using System.Net.Http.Headers;

namespace RecommendationService.Services
{
    public class CollaborativeRecommendationService
    {
        private readonly MLContext _mlContext;
        private readonly HttpClient _httpClient;
        private readonly string _userServiceUrl;
        private readonly string _recipeServiceUrl;
        private ITransformer _model;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CollaborativeRecommendationService(HttpClient httpClient, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _mlContext = new MLContext();
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

    
        public async Task<List<RecipeDTO>> GetCollaborativeRecommendations(Guid userId, int limit = 5)
        {
            // 1. Get All Raitings
            var ratings = await _httpClient.GetFromJsonAsync<List<RecipeRating>>(
                $"{_recipeServiceUrl}/api/rating/get-user-ratings");

            if (ratings == null || !ratings.Any())
                return new List<RecipeDTO>();

            // 2. Mapping UserId and RecipeId in uint
            var userIdMap = ratings
                .Select(r => r.UserId)
                .Distinct()
                .Select((id, index) => new { id, index })
                .ToDictionary(x => x.id, x => (uint)x.index);

            var recipeIdMap = ratings
                .Select(r => r.RecipeId)
                .Distinct()
                .Select((id, index) => new { id, index })
                .ToDictionary(x => x.id, x => (uint)x.index);

            if (!userIdMap.ContainsKey(userId))
                return new List<RecipeDTO>();

            // 3. Prepare data
            var mappedData = ratings
                .Select(r => new RatingEntry
                {
                    UserId = userIdMap[r.UserId],
                    ItemId = recipeIdMap[r.RecipeId],
                    Label = r.Rating
                })
                .ToList();

            var dataView = _mlContext.Data.LoadFromEnumerable(mappedData);

            // 4. Configure and training model 
            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = nameof(RatingEntry.UserId),
                MatrixRowIndexColumnName = nameof(RatingEntry.ItemId),
                LabelColumnName = nameof(RatingEntry.Label),
                NumberOfIterations = 20,
                ApproximationRank = 100
            };

            var trainer = _mlContext.Recommendation().Trainers.MatrixFactorization(options);
            var model = trainer.Fit(dataView);

            // 5. Get All Recipes
            var response = await _httpClient.GetFromJsonAsync<APIResponse<PagedResponse<RecipeDTO>>>(
                $"{_recipeServiceUrl}/api/recipes?pageNumber=1&pageSize=100");

            var allRecipes = response?.Result?.Items ?? new List<RecipeDTO>();

            // 6. Predicting
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<RatingEntry, RatingPrediction>(model);
            var predictions = new List<(Guid RecipeId, float Score)>();

            foreach (var recipe in allRecipes)
            {
                if (!recipeIdMap.TryGetValue(recipe.Id, out uint recipeIndex))
                    continue;

                var prediction = predictionEngine.Predict(new RatingEntry
                {
                    UserId = userIdMap[userId],
                    ItemId = recipeIndex
                });

                predictions.Add((recipe.Id, prediction.Score));
            }

            // 7. Select top-N
            var topRecipeIds = predictions
                .OrderByDescending(p => p.Score)
                .Take(limit)
                .Select(p => p.RecipeId)
                .ToList();

            return allRecipes
                .Where(r => topRecipeIds.Contains(r.Id))
                .ToList();
        }

    }
}
