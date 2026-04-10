using Souq.Models;
using Souq.Services.Interfaces;
using Souq.UnitOfWork;
using Souq.ViewModels.Cart;

namespace Souq.Services.Implementations
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _uow;

        public CartService(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<int> AddToCartAsync(int variationId, int quantity, string? userId, string? sessionId)
        {
            CartItem? existing = null;
            if(userId != null)
            {
                var userCart = await _uow.Cart.GetCartByUserAsync(userId);
                existing = userCart.FirstOrDefault(i => i.VariationId == variationId);
            }
            else if(sessionId != null)
            {
                var sessionCart = await _uow.Cart.GetCartBySessionAsync(sessionId);
                existing = sessionCart.FirstOrDefault(i => i.VariationId == variationId);
            }

            if(existing != null)
            {
                existing.Quantity += quantity;
                _uow.Cart.Update(existing);
            }
            else
            {
                var newItem = new CartItem
                {
                    UserId = userId,
                    SessionId = userId == null ? sessionId : null,
                    VariationId = variationId,
                    Quantity = quantity,
                    AddedAt = DateTime.UtcNow
                };
                await _uow.Cart.AddAsync(newItem);
            }

            await _uow.SaveAsync();
            return await GetCartCountAsync(userId, sessionId);
        }

        public async Task<CartViewModel> GetCartAsync(string? userId, string? sessionId)
        {
            var items = userId != null
                ? await _uow.Cart.GetCartByUserAsync(userId)
                : sessionId != null
                  ? await _uow.Cart.GetCartBySessionAsync(sessionId)
                  : new List<CartItem>();

            var viewModel = new CartViewModel
            {
                Items = items.Select(item => new CartItemViewModel
                {
                    CartItemId = item.Id,
                    ProductId = item.Variation.Product.Id,
                    ProductName = item.Variation.Product.Name,
                    ProductSlug = item.Variation.Product.Slug,
                    VendorName = item.Variation.Product.Vendor.StoreName,
                    VariationId = item.VariationId,
                    VariationName = item.Variation.Name,
                    Color = item.Variation.Color,
                    Size = item.Variation.Size,
                    UnitPrice = item.Variation.Price,
                    MaxStock = item.Variation.AvailableStock,
                    Quantity = item.Quantity,
                    ImageUrl = item.Variation.ImageUrl
                              ?? item.Variation.Product.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl
                              ?? item.Variation.Product.Images.FirstOrDefault()?.ImageUrl

                }).ToList()
            };
                return viewModel;
        }

        public async Task<int> GetCartCountAsync(string? userId, string? sessionId)
        {
            if (userId != null)
            {
                var items = await _uow.Cart.FindAsync(c => c.UserId == userId);
                return items.Sum(i => i.Quantity);
            }
            else if (sessionId != null)
            {
                var items = await _uow.Cart.FindAsync(c => c.SessionId == sessionId);
                return items.Sum(i => i.Quantity);
            }
            return 0;
        }

        public async Task RemoveFromCartAsync(int cartItemId, string? userId, string? sessionId)
        {
            var item = await _uow.Cart.GetByIdAsync(cartItemId);
            if(item == null) return;

            bool isOwner = (userId != null && item.UserId == userId) || (sessionId != null && item.SessionId == sessionId);
            if(!isOwner) return;

            _uow.Cart.Remove(item);
            await _uow.SaveAsync();
        }

        public async Task<int> UpdateQuantityAsync(int cartItemId, int quantity, string? userId, string? sessionId)
        {
            var item = await _uow.Cart.GetByIdAsync(cartItemId);
            if (item == null) return await GetCartCountAsync(userId,sessionId);

            bool isOwner = (userId != null && item.UserId == userId) || (sessionId != null && item.SessionId == sessionId);
            if (!isOwner) return await GetCartCountAsync(userId, sessionId);

            if(quantity <= 0)
            {
                _uow.Cart.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
                _uow.Cart.Update(item);
            }

            
            await _uow.SaveAsync();
            return await GetCartCountAsync(userId, sessionId);
        }

        public async Task ClearCartAsync(string userId)
        {
            await _uow.Cart.ClearCartAsync(userId);
            await _uow.SaveAsync();
        }
    }
}
