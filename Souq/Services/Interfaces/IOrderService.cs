using Souq.Models;
using Souq.Models.Enums;
using Souq.ViewModels.Checkout;
using Souq.ViewModels.Orders;

namespace Souq.Services.Interfaces
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(CheckoutViewModel model,
                                     string userId);
        Task<string> CreateStripeSessionAsync(Order order,
                                              string successUrl,
                                              string cancelUrl);
        Task HandleWebhookAsync(string json, string stripeSignature);
        Task<OrderSuccessViewModel?> GetOrderSuccessAsync(int orderId);
        Task<bool> IsOrderPaidAsync(int orderId);


        Task<List<OrderListItemViewModel>> GetCustomerOrderAsync(string userId);
        Task<OrderDetailViewModel?> GetCustomerOrderDetailAsync(int orderId, string userId);
        Task<List<OrderListItemViewModel>> GetVendorOrdersAsync(int vendorId);
        Task<OrderDetailViewModel?> GetVendorOrderDetailAsync(int orderId, int vendorId);
        Task<bool> UpdateOrderStatusAsync(int orderId, int vendorId, OrderStatus newStatus);
    }
}
