using Nest;

namespace RecipeService.Models
{
    public class RecipeSearchService
    {
        private readonly IElasticClient _client;
        public RecipeSearchService(IConfiguration configuration)
        {
            var settings = new ConnectionSettings(new Uri(configuration["Elasticsearch:Uri"]))
                .DefaultIndex("recipes");
            _client = new ElasticClient(settings);
        }
        public async Task IndexRecipeAsync(Recipe recipe)
        {
            await _client.IndexDocumentAsync(recipe);
        }
        public async Task<IEnumerable<Recipe>> SearchAsync(string query)
        {
            var response = await _client.SearchAsync<Recipe>(s => s
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f.Field(f => f.Title).Field(f => f.Description))
                        .Query(query))));
            return response.Documents;
        }
    }
}
