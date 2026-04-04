namespace Souq.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Color { get; set; }
        public bool IsMain { get; set; } = false;
        public int SortOrder { get; set; }
        // Navigation property
        public Product Product { get; set; } = null!;
    }
}
