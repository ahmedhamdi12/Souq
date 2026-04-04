using Souq.Models;

namespace Souq.Repositories.Interfaces
{
    public interface ICartRepository : IGenericRepository<CartItem>
    {
            Task<IEnumerable<CartItem>> GetCartByUserAsync(string userId);
            Task<IEnumerable<CartItem>> GetCartBySessionAsync(string sessionId);
            Task MergeGuestCartAsync(string sessionId, string userId);
            Task ClearCartAsync(string userId);
    }
}
