namespace RecipeService.Models.Filter
{
    public class CheckboxFilter
    {
        public List<string>? Diets { get; set; }
        public List<string>? Allergens { get; set; }
        public List<string>? Cuisines { get; set; }
        public List<string>? Tags { get; set; }
    }
}
