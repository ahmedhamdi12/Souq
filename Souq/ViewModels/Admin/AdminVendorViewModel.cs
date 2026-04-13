using Souq.Models.Enums;

namespace Souq.ViewModels.Admin
{
    public class AdminVendorViewModel
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string StoreSlug { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public VendorStatus Status { get; set; }
        public string StatusColor { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal TotalEarnings { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
