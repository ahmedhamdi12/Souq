using Souq.Models.Enums;

namespace Souq.Models
{
    public class VendorProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string StoreSlug { get; set; } = string.Empty;
        public string? Description { get; set; } 
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? StripeAccountId { get; set; }
        public VendorStatus Status { get; set; } = VendorStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<VendorPayout> Payouts { get; set; } = new List<VendorPayout>();

    }
}
