namespace RecipeService.Models.Categories.DTOs
{
    public class CategoryCreateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public Guid? ParentCategoryId { get; set; }
    }
}
