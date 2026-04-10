namespace Souq.ViewModels.Orders
{
    public class OrderDetailViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;

        // Shipping info
        public string ShippingFullName { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingCountry { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;

        // Items
        public List<OrderItemViewModel> Items { get; set; } = new();

        // Totals
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }

        // Vendor specific
        public decimal VendorEarnings { get; set; }
        public bool CanUpdateStatus { get; set; }
        public List<string> AvailableStatusTransitions { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public int OrderItemId { get; set; }
        public string? ImageUrl { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string VariationName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
        public decimal VendorEarnings { get; set; }
        public string VendorName { get; set; } = string.Empty;
    }
}
