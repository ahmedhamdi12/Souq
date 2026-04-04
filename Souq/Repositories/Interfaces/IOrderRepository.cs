using Souq.Models;

namespace Souq.Repositories.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetOrderWithItemsAsync(int id);
        Task<IEnumerable<Order>> GetOrdersByUserAsync(string userId);
        
         Task<IEnumerable<Order>> GetOrdersByVendorAsync(int vendorId);
        Task<Order?> GetByStripeSessionAsync(string sessionId);
    }
}
