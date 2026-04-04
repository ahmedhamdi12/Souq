namespace Souq.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; } 
        public decimal BasePrice { get; set; }
        public bool HasVariations { get; set; } = false;
        public bool IsApproved { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Navigation properties
        public VendorProfile Vendor { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public ICollection<ProductVariation> Variations { get; set; } = new List<ProductVariation>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
