using AuthService.Models;

namespace AuthService.Repository
{
    public interface IUserPreferencesRepository
    {
        Task<UserPreferences?> GetByUserIdAsync(Guid userId);
        Task SaveAsync(UserPreferences preferences);
    }

}
