using Souq.Models;
using Souq.Services.Interfaces;
using Souq.UnitOfWork;
using Souq.ViewModels.Products;

namespace Souq.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;
        public ProductService(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<List<Department>> GetAllDepartmentsAsync()
        {
            var departments =await _uow.Departments.GetAllAsync();
            return departments.ToList();
        }

        public async Task<List<Product>> GetFeaturedProductsAsync(int count = 8)
        {
            var products = await _uow.Products.GetApprovedProductAsync();
            return products.Take(count).ToList();
        }

        public async Task<ProductDetailViewModel?> GetProductDetailAsync(string slug)
        {
            var product = await _uow.Products.GetBySlugAsync(slug);
            if (product == null) return null;

            // Get full details including reviews
            var fullProduct = await _uow.Products
                .GetProductWithDetailsAsync(product.Id);
            if (fullProduct == null) return null;

            var viewModel = new ProductDetailViewModel
            {
                Product = fullProduct
            };

            // Product images grouped by color
            foreach (var image in fullProduct.Images)
            {
                var colorKey = image.Color ?? string.Empty;
                if (!viewModel.ImagesByColor.ContainsKey(colorKey))
                    viewModel.ImagesByColor[colorKey] = new List<string>();
                viewModel.ImagesByColor[colorKey].Add(image.ImageUrl);
            }

            // Variation images grouped by color
            foreach (var variation in fullProduct.Variations)
            {
                if (variation.ImageUrl == null) continue;
                var colorKey = variation.Color ?? string.Empty;
                if (!viewModel.ImagesByColor.ContainsKey(colorKey))
                    viewModel.ImagesByColor[colorKey] = new List<string>();
                if (!viewModel.ImagesByColor[colorKey].Contains(variation.ImageUrl))
                    viewModel.ImagesByColor[colorKey].Add(variation.ImageUrl);
            }

            // All images flattened for default display
            viewModel.AllImages = fullProduct.Images
                .OrderByDescending(i => i.IsMain)
                .Select(i => i.ImageUrl)
                .ToList();
            // Add variation images not already included
            foreach (var v in fullProduct.Variations)
            {
                if (v.ImageUrl != null && !viewModel.AllImages.Contains(v.ImageUrl))
                    viewModel.AllImages.Add(v.ImageUrl);
            }

            // Variations grouped by color for swatches
            viewModel.VariationsByColor = fullProduct.Variations
                .GroupBy(v => v.Color ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Related products
            viewModel.RelatedProducts = await GetRelatedProductsAsync(
                fullProduct.CategoryId, fullProduct.Id);

            return viewModel;
        }

        public async Task<List<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int count = 4)
        {
            var products = await _uow.Products.GetProductsByCategoryIdAsync(categoryId);
            return products
                .Where(p => p.Id != excludeProductId)
                .Take(count)
                .ToList();
        }
    }
}
