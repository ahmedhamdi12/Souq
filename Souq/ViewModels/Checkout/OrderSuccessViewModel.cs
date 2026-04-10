namespace Souq.ViewModels.Checkout
{
    public class OrderSuccessViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public bool IsPaid { get; set; }
    }
}
