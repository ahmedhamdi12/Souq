using Souq.ViewModels.Cart;

namespace Souq.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartViewModel> GetCartAsync(string? userId, string? sessionId);
        Task<int> AddToCartAsync(int variationId, int quantity, string? userId, string? sessionId);
        Task<int> UpdateQuantityAsync(int cartItemId, int quantity, string? userId, string? sessionId);
        Task RemoveFromCartAsync(int  cartItemId, string? userId, string? sessionId);
        Task<int> GetCartCountAsync(string? userId, string? sessionId);
    }
}
