using Souq.Models;

namespace Souq.ViewModels.Store
{
    public class StoreViewModel
    {
        public VendorProfile Vendor { get; set; } = null!;
        public List<Product> Products { get; set; } = new();
        public int TotalProducts { get; set; }
        public int TotalSales { get; set; }
        public double AverageRating { get; set; }

        /*
            These are computed from the vendor's order items.
            Used to show social proof on the store page.
        */
        public int TotalReviews { get; set; }
    }
}
