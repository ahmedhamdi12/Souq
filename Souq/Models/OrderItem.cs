using Souq.Models.Enums;

namespace Souq.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int VariationId { get; set; }
        public int VendorId { get; set; }

        //Snapshot ,never change this after order is placed
        public string ProductName { get; set; } = string.Empty;
        public string VariationName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal VendorEarnings { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Paid;

        // Navigation properties
        public Order Order { get; set; } = null!;
        public ProductVariation Variation { get; set; } = null!;
        public VendorProfile Vendor { get; set; } = null!;
    }
}
