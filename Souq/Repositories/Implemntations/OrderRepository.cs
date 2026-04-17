using Microsoft.EntityFrameworkCore;
using Souq.Data;
using Souq.Models;
using Souq.Repositories.Interfaces;

namespace Souq.Repositories.Implemntations
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context) : base(context)
        {
        }
        public async Task<Order?> GetByStripeSessionAsync(string sessionId)
        {
            return await _dbSet.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.StripeSessionId == sessionId);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserAsync(string userId)
        {
            return await _dbSet
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variation)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersByVendorAsync(int vendorId)
        {
            return await _dbSet
                .Where(o => o.OrderItems.Any(oi => oi.VendorId == vendorId))
                .Include(o => o.OrderItems.Where(oi => oi.VendorId == vendorId))
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderWithItemsAsync(int id)
        {
            return await _dbSet
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Vendor)
                .Include(o => o.OrderItems)
                     .ThenInclude(oi => oi.Variation)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
        }
    }
}
