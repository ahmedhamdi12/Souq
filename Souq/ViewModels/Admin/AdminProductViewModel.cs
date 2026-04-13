namespace Souq.ViewModels.Admin
{
    public class AdminProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
