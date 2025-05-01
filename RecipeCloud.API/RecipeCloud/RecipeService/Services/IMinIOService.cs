namespace RecipeService.Services
{
    public interface IMinIOService
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task DeleteImageAsync(string fileName);
    }
}
