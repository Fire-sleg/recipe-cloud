
using AuthService.Data;
using AuthService.Models;
using AuthService.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repository
{
    public class ViewHistoryRepository : IViewHistoryRepository
    {
        private readonly ApplicationDbContext _db;
        public ViewHistoryRepository(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task SendViewHistoryAsync(Guid recipeId, Guid userId)
        {
            if (recipeId != Guid.Empty && userId != Guid.Empty)
            {
                var viewHistory = new ViewHistory { RecipeId = recipeId, UserId = userId, ViewedAt = DateTime.UtcNow };

                await _db.ViewHistories.AddAsync(viewHistory);
                await SaveAsync();
            }
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task<List<ViewHistory>> GetViewHistory(Guid userId, int limit)
        {
            IQueryable<ViewHistory> query = _db.ViewHistories
                .Where(vh => vh.UserId == userId)
                .OrderBy(vh=>vh.ViewedAt)
                .Take(limit);

            return await query.ToListAsync();

        }
    }
}
