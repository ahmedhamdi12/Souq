using Souq.Models;
using Souq.Models.Enums;
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

        public async Task<bool> AddReviewAsync(ReviewViewModel model, string userId)
        {
            var canReview = await CanUserReviewAsync(model.ProductId, userId);
            if (!canReview) return false;

            var review = new Models.Review
            {
                ProductId = model.ProductId,
                UserId = userId,
                Rating = model.Rating,
                Comment = model.Comment,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Reviews.AddAsync(review);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<bool> CanUserReviewAsync(int productId, string userId)
        {
            var orders = await _uow.Orders.GetOrdersByUserAsync(userId);
            var hasPurchased = orders.
                Where(o => o.Status == OrderStatus.Delivered)
                .SelectMany(o => o.OrderItems)
                .Any(oi => oi.Variation.ProductId == productId);
            if (!hasPurchased) return false;

            var existingReview = await _uow.Reviews.FindAsync(r => r.ProductId == productId && r.UserId == userId);

            return !existingReview.Any();
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

        public async Task<ProductsListViewModel> GetProductsListAsync(string? search, string? departmentSlug, string? categorySlug, string sortBy, int page, int pageSize)
        {
            var allProducts = await _uow.Products.GetApprovedProductAsync();
            var departments = await _uow.Departments.GetAllAsync();

            var query = allProducts.AsQueryable();
            if(!string.IsNullOrWhiteSpace(search))
            {
                var q = search.ToLower();
                query = query.Where
                    (p => p.Name.ToLower().Contains(q) ||
                    (p.Description !=null && p.Description.ToLower().Contains(q)) ||
                    (p.Vendor.StoreName != null && p.Vendor.StoreName.ToLower().Contains(q)));
            }

            // Apply department filter
            if (!string.IsNullOrWhiteSpace(departmentSlug))
            {
                query = query.Where(p =>
                    p.Category.Department.Slug == departmentSlug);
            }

            // Apply category filter
            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                query = query.Where(p =>
                    p.Category.Slug == categorySlug);
            }

            // Apply sorting
            query = sortBy switch
            {
                "price-asc" => query.OrderBy(p =>
                    p.HasVariations && p.Variations.Any()
                        ? p.Variations.Min(v => v.Price)
                        : p.BasePrice),
                "price-desc" => query.OrderByDescending(p =>
                    p.HasVariations && p.Variations.Any()
                        ? p.Variations.Min(v => v.Price)
                        : p.BasePrice),
                "name" => query.OrderBy(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalProducts = query.Count();
            var totalPages = (int)Math.Ceiling(
                totalProducts / (double)pageSize);

            // Apply pagination
            var products = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            return new ProductsListViewModel
            {
                Products = products,
                Departments = departments.ToList(),
                SearchQuery = search,
                DepartmentSlug = departmentSlug,
                CategorySlug = categorySlug,
                SortBy = sortBy,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalProducts = totalProducts,
                PageSize = pageSize
            };
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
