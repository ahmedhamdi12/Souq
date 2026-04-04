using Souq.Models.Enums;

namespace Souq.Models
{
    public class VendorPayout
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public decimal Amount { get; set; }
        public PayoutStatus Status { get; set; } = PayoutStatus.Pending;
        public string? StripeTransferId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public VendorProfile Vendor { get; set; } = null!;
    }
}
