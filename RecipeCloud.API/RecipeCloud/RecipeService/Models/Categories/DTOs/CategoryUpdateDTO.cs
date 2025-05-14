namespace RecipeService.Models.Categories.DTOs
{
    public class CategoryUpdateDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        //public string TransliteratedName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        //public List<string>? BreadcrumbPath { get; set; }
        public Guid? ParentCategoryId { get; set; }
    }
}
