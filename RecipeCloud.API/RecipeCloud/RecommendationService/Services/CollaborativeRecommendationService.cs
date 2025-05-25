using Microsoft.ML;
using Microsoft.ML.Trainers;
using RecommendationService.Models;

namespace RecommendationService.Services
{
    public class CollaborativeRecommendationService
    {
        private readonly MLContext _mlContext;
        private readonly HttpClient _httpClient;
        private readonly string _userServiceUrl;
        private readonly string _recipeServiceUrl;
        private ITransformer _model;

        public CollaborativeRecommendationService(HttpClient httpClient, IConfiguration config)
        {
            _mlContext = new MLContext();
            _httpClient = httpClient;
            _userServiceUrl = config["ServiceUrls:UserService"];
            _recipeServiceUrl = config["ServiceUrls:RecipeService"];
        }

        public async Task<List<RecipeDTO>> GetCollaborativeRecommendations(Guid userId, int limit = 5)
        {
            // 1. Завантажити оцінки
            var ratings = await _httpClient.GetFromJsonAsync<List<RatingEntry>>(
                $"{_userServiceUrl}/api/users/ratings");

            // 2. Навчити модель
            var data = _mlContext.Data.LoadFromEnumerable(ratings);
            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = "UserId",
                MatrixRowIndexColumnName = "RecipeId",
                LabelColumnName = "Rating",
                NumberOfIterations = 20,
                ApproximationRank = 100
            };
            var trainer = _mlContext.Recommendation().Trainers.MatrixFactorization(options);
            _model = trainer.Fit(data);

            // 3. Прогнозувати оцінки
            var allRecipes = await _httpClient.GetFromJsonAsync<List<RecipeDTO>>(
                $"{_recipeServiceUrl}/api/recipes");
            var predictions = new List<(Guid RecipeId, float Score)>();

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<RatingEntry, RatingPrediction>(_model);
            foreach (var recipe in allRecipes)
            {
                var prediction = predictionEngine.Predict(new RatingEntry
                {
                    UserId = userId,
                    ItemId = recipe.Id
                });
                predictions.Add((recipe.Id, prediction.Score));
            }

            // 4. Вибрати топ-N
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
