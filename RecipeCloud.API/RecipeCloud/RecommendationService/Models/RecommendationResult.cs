namespace RecommendationService.Models
{
    public class RecommendationResult
    {
        public List<RecipeDTO> Recommendations { get; set; }
        public Dictionary<string, double> Metrics { get; set; }
    }
}
