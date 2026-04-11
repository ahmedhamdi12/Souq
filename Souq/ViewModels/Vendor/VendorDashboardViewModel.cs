namespace Souq.ViewModels.Vendor
{
    public class VendorDashboardViewModel
    {
        public string StoreName { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public int PendingApproval { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal MonthEarnings { get; set; }
        public List<RecentOrderItem> RecentOrders { get; set; } = new();
    }

    public class RecentOrderItem
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Earnings { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
