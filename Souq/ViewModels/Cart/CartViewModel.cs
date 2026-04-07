namespace Souq.ViewModels.Cart
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal Subtotal => Items.Sum(i => i.LineTotal);
        public int TotalItems => Items.Sum(i => i.Quantity);
        public bool IsEmpty => !Items.Any();

    }
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSlug { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public int VariationId { get; set; }
        public string VariationName { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Size { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int MaxStock { get; set; }

        // Computed — total for this line
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
