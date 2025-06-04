using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repository
{
    public class UserPreferencesRepository : IUserPreferencesRepository
    {
        private readonly ApplicationDbContext _context;

        public UserPreferencesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserPreferences?> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }
        public async Task SaveAsync(UserPreferences preferences)
        {
            var existing = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == preferences.UserId);
            if (existing != null)
            {
                existing.Allergens = preferences.Allergens;
                existing.DietaryPreferences = preferences.DietaryPreferences;
                existing.FavoriteCuisines = preferences.FavoriteCuisines;
            }
            else
            {
                _context.UserPreferences.Add(preferences);
            }

            await _context.SaveChangesAsync();
        }
    }

}
