using Microsoft.EntityFrameworkCore;
using Souq.Data;
using Souq.Models;
using Souq.Repositories.Interfaces;

namespace Souq.Repositories.Implemntations
{
    public class CartRepository : GenericRepository<CartItem>, ICartRepository
    {
        public CartRepository(AppDbContext context) : base(context) { }

        public async Task ClearCartAsync(string userId)
        {
            var items = await _dbSet.Where(c => c.UserId == userId).ToListAsync();
            _dbSet.RemoveRange(items);
        }

        public async Task<IEnumerable<CartItem>> GetCartBySessionAsync(string sessionId)
        {
            return await _dbSet.Where(c => c.SessionId == sessionId)
                .Include(c => c.Variation)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
                .Include(c => c.Variation)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Vendor)
                .ToListAsync();
        }

        public async Task<IEnumerable<CartItem>> GetCartByUserAsync(string userId)
        {
            return await _dbSet.Where(c => c.UserId == userId)
                .Include(c => c.Variation)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
                .Include(c => c.Variation)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Vendor)
                .ToListAsync();
        }

        public async Task MergeGuestCartAsync(string sessionId, string userId)
        {
            var guestItems = await _dbSet.Where(c => c.SessionId == sessionId).ToListAsync();

            if (!guestItems.Any()) return;

            var userItems =await _dbSet.Where(c => c.UserId == userId).ToListAsync();

            foreach (var guestItem in guestItems)
            {
                var existing = userItems.FirstOrDefault(u => u.VariationId == guestItem.VariationId);
                if (existing != null)
                { 
                    existing.Quantity += guestItem.Quantity;
                    _dbSet.Remove(guestItem);
                }
                else
                {
                    guestItem.UserId = userId;
                    guestItem.SessionId = null;
                }
            }
        }
    }
}
