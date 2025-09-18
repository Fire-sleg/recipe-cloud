namespace RecipeService.Services
{
    public interface IMinIOService
    {
        Task<string> UploadImageAsync(IFormFile file, CancellationToken cancellationToken);
        Task DeleteImageAsync(string fileName, CancellationToken cancellationToken);
    }
}
