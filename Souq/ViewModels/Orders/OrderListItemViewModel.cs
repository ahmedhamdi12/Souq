namespace Souq.ViewModels.Orders
{
    public class OrderListItemViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
        public string? CustomerName { get; set; }  // vendor sees this
    }
}
