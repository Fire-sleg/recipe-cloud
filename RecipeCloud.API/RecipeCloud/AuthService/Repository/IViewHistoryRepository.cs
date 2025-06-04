using AuthService.Models;
using AuthService.Models.DTOs;

namespace AuthService.Repository
{
    public interface IViewHistoryRepository
    {
        Task SendViewHistoryAsync(Guid recipeId, Guid userId);
        Task<List<ViewHistory>> GetViewHistory(Guid userId, int limit);
    }
}
