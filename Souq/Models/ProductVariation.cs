namespace Souq.Models
{
    public class ProductVariation
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int ReservedQuantity { get; set; } = 0;
        public string? Color { get; set; }
        public string? Size { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public int AvailableStock => StockQuantity - ReservedQuantity;


        // Navigation properties
        public Product Product { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
