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

        //public async Task<List<RecipeDTO>> GetCollaborativeRecommendations(Guid userId, int limit = 5)
        //{
        //    // 1. Завантажити оцінки  list RatingEntry
        //    var ratings = await _httpClient.GetFromJsonAsync<List<RecipeRating>>(
        //        $"{_recipeServiceUrl}/api/rating/get-user-ratings");

        //    // 2. Навчити модель
        //    //var ratingsML = ratings.Select(r => new RecipeRatingML
        //    //{
        //    //    UserId = r.UserId.ToString(),
        //    //    RecipeId = r.RecipeId.ToString(),
        //    //    Rating = r.Rating
        //    //}).ToList();

        //    //var data = _mlContext.Data.LoadFromEnumerable(ratingsML);
        //    //var options = new MatrixFactorizationTrainer.Options
        //    //{
        //    //    MatrixColumnIndexColumnName = "UserId",
        //    //    MatrixRowIndexColumnName = "RecipeId",
        //    //    LabelColumnName = "Rating",
        //    //    NumberOfIterations = 20,
        //    //    ApproximationRank = 100
        //    //};
        //    //var trainer = _mlContext.Recommendation().Trainers.MatrixFactorization(options);
        //    //_model = trainer.Fit(data);
        //    var ratingsML = ratings.Select(r => new RecipeRatingML
        //    {
        //        UserId = r.UserId.ToString(),
        //        RecipeId = r.RecipeId.ToString(),
        //        Rating = r.Rating
        //    }).ToList();


        //    var userIdMap = ratingsML
        //        .Select(r => r.UserId)
        //        .Distinct()
        //        .Select((id, index) => new { id, index })
        //        .ToDictionary(x => x.id, x => (uint)x.index);

        //    var recipeIdMap = ratingsML
        //        .Select(r => r.RecipeId)
        //        .Distinct()
        //        .Select((id, index) => new { id, index })
        //        .ToDictionary(x => x.id, x => (uint)x.index);

        //    var mappedData = ratingsML
        //        .Select(r => new RatingEntry
        //        {
        //            UserId = userIdMap[r.UserId],
        //            ItemId = recipeIdMap[r.RecipeId],
        //            Label = r.Rating
        //        })
        //        .ToList();


        //    var dataView = _mlContext.Data.LoadFromEnumerable(mappedData);

        //    var options = new MatrixFactorizationTrainer.Options
        //    {
        //        MatrixColumnIndexColumnName = nameof(RatingEntry.UserId),
        //        MatrixRowIndexColumnName = nameof(RatingEntry.ItemId),
        //        LabelColumnName = nameof(RatingEntry.Label),
        //        NumberOfIterations = 20,
        //        ApproximationRank = 100
        //    };

        //    var trainer = _mlContext.Recommendation().Trainers.MatrixFactorization(options);
        //    var model = trainer.Fit(dataView); // ← тепер модель не буде null


        //    //var data = _mlContext.Data.LoadFromEnumerable(ratingsML);

        //    //// Create pipeline: map UserId and RecipeId to keys
        //    //var dataProcessPipeline = _mlContext.Transforms
        //    //    .Conversion.MapValueToKey("UserIdEncoded", "UserId")
        //    //    .Append(_mlContext.Transforms.Conversion.MapValueToKey("RecipeIdEncoded", "RecipeId"));

        //    //// Fit pipeline and transform data
        //    //var transformedData = dataProcessPipeline
        //    //    .Fit(data)
        //    //    .Transform(data);

        //    //// MatrixFactorization options
        //    //var options = new MatrixFactorizationTrainer.Options
        //    //{
        //    //    MatrixColumnIndexColumnName = "UserIdEncoded",
        //    //    MatrixRowIndexColumnName = "RecipeIdEncoded",
        //    //    LabelColumnName = "Rating",
        //    //    NumberOfIterations = 20,
        //    //    ApproximationRank = 100
        //    //};

        //    //var trainer = _mlContext.Recommendation().Trainers.MatrixFactorization(options);
        //    //var model = trainer.Fit(transformedData);


        //    // 3. Прогнозувати оцінки
        //    //var allRecipes = await _httpClient.GetFromJsonAsync<List<RecipeDTO>>(
        //    //    $"{_recipeServiceUrl}/api/recipes");


        //    var response = await _httpClient.GetFromJsonAsync<APIResponse<PagedResponse<RecipeDTO>>>(
        //        $"{_recipeServiceUrl}/api/recipes?pageNumber=1&pageSize=100");

        //    var allRecipes = response?.Result?.Items ?? new List<RecipeDTO>();


        //    var predictions = new List<(Guid RecipeId, float Score)>();

        //    var predictionEngine = _mlContext.Model.CreatePredictionEngine<RatingEntry, RatingPrediction>(_model);
        //    foreach (var recipe in allRecipes)
        //    {
        //        var prediction = predictionEngine.Predict(new RatingEntry
        //        {
        //            UserId = userId,
        //            ItemId = recipe.Id
        //        });
        //        predictions.Add((recipe.Id, prediction.Score));
        //    }

        //    // 4. Вибрати топ-N
        //    var topRecipeIds = predictions
        //        .OrderByDescending(p => p.Score)
        //        .Take(limit)
        //        .Select(p => p.RecipeId)
        //        .ToList();

        //    return allRecipes
        //        .Where(r => topRecipeIds.Contains(r.Id))
        //        .ToList();
        //}
        public async Task<List<RecipeDTO>> GetCollaborativeRecommendations(Guid userId, int limit = 5)
        {
            // 1. Завантаження всіх рейтингів
            var ratings = await _httpClient.GetFromJsonAsync<List<RecipeRating>>(
                $"{_recipeServiceUrl}/api/rating/get-user-ratings");

            if (ratings == null || !ratings.Any())
                return new List<RecipeDTO>();

            // 2. Мапінг UserId і RecipeId у uint
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

            // Якщо користувача немає у мапі — повернути порожній список
            if (!userIdMap.ContainsKey(userId))
                return new List<RecipeDTO>();

            // 3. Підготовка даних
            var mappedData = ratings
                .Select(r => new RatingEntry
                {
                    UserId = userIdMap[r.UserId],
                    ItemId = recipeIdMap[r.RecipeId],
                    Label = r.Rating
                })
                .ToList();

            var dataView = _mlContext.Data.LoadFromEnumerable(mappedData);

            // 4. Налаштування та тренування моделі
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

            // 5. Отримання всіх рецептів
            var response = await _httpClient.GetFromJsonAsync<APIResponse<PagedResponse<RecipeDTO>>>(
                $"{_recipeServiceUrl}/api/recipes?pageNumber=1&pageSize=100");

            var allRecipes = response?.Result?.Items ?? new List<RecipeDTO>();

            // 6. Прогнозування
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

            // 7. Вибрати топ-N
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
