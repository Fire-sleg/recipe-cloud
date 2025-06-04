namespace AuthService.Models.DTOs
{
    public class ViewHistoryDTO
    {
        public Guid UserId { get; set; }
        public Guid RecipeId { get; set; }
        public Guid CollectionId { get; set; }
    }
}
