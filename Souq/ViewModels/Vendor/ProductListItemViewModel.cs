namespace Souq.ViewModels.Vendor
{
    public class ProductListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal BasePrice { get; set; }
        public bool HasVariations { get; set; }
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public int TotalStock { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
