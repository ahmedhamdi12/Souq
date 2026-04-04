using Souq.Models.Enums;

namespace Souq.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public OrderStatus Status { get; set; } = OrderStatus.Draft;
        public decimal SubTotal { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal Total { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public string? StripeSessionId { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //Shipping Snapshot
        public string ShippingFullName { get; set; } = string.Empty;
        public string ShippingAddressLine { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingCountry { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;


        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
