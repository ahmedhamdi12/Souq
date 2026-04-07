using Souq.Models;

namespace Souq.ViewModels.Products
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public Dictionary<string, List<string>> ImagesByColor { get; set; } = new();
        public List<string> AllImages { get; set; } = new();
        public Dictionary<string, List<ProductVariation>> VariationsByColor
        { get; set; } = new();
        public decimal MinPrice => Product.HasVariations
            && Product.Variations.Any()
            ? Product.Variations.Min(v => v.Price)
            : Product.BasePrice;

        public decimal MaxPrice => Product.HasVariations
            && Product.Variations.Any()
            ? Product.Variations.Max(v => v.Price)
            : Product.BasePrice;

        public bool HasPriceRange => MinPrice != MaxPrice;
        public List<Product> RelatedProducts { get; set; } = new();

    }
}
