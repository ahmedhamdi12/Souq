namespace Souq.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string? UserId { get; set; } 
        public string? SessionId { get; set; }
        public int VariationId { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;


        // Navigation properties
        public ApplicationUser? User { get; set; } 
        public ProductVariation Variation { get; set; } = null!;
    }
}
