using RecipeService.Models.Pagination;

namespace RecipeService.Models.Filter
{
    public class RecipeFilterRequest
    {
        public RecipeFilterDTO FilterDto { get; set; } = new RecipeFilterDTO();
        public PaginationParams PaginationParams { get; set; } = new PaginationParams();
    }
}
