namespace Souq.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalVendors { get; set; }
        public int PendingVendors { get; set; }
        public int TotalProducts { get; set; }
        public int PendingProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal PlatformEarnings { get; set; }
        public List<RecentActivityItem> RecentActivity { get; set; } = new();
    }

    public class RecentActivityItem
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
