using Microsoft.Extensions.Caching.Memory;
using Souq.Models;
using Souq.Services.Interfaces;
using Souq.ViewModels.Products;

namespace Souq.Services.Implementations
{
    public class CachedProductService : IProductService
    {
        private readonly ProductService _inner;
        private readonly IMemoryCache _cache;

        /*
            This is the Decorator pattern.
            CachedProductService wraps ProductService.
            For methods that benefit from caching
            we check cache first.
            For everything else we call through to inner.
        */
        public CachedProductService(ProductService inner, IMemoryCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public Task<bool> AddReviewAsync(ReviewViewModel model, string userId)
        => _inner.AddReviewAsync(model, userId);

        public Task<bool> CanUserReviewAsync(int productId, string userId)
        => _inner.CanUserReviewAsync(productId, userId);

        public async Task<List<Department>> GetAllDepartmentsAsync()
        {
            const string cacheKey = "departments_all";
            /*
                GetOrCreateAsync:
                1. Check if "departments_all" is in cache
                2. If yes → return cached value instantly
                3. If no → call the factory function,
                   store result in cache, return it
            */
            return await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    /*
                        Cache departments for 1 hour.
                        Departments rarely change so this
                        is safe and saves many DB queries.
                    */
                    entry.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromHours(1);

                    return await _inner.GetAllDepartmentsAsync();
                }) ?? new List<Department>();
        }

        public Task<List<Product>> GetFeaturedProductsAsync(int count = 8)
        => _inner.GetFeaturedProductsAsync(count);

        public Task<ProductDetailViewModel?> GetProductDetailAsync(string slug)
        => _inner.GetProductDetailAsync(slug);

        public Task<ProductsListViewModel> GetProductsListAsync(string? search, string? departmentSlug, string? categorySlug, string sortBy, int page, int pageSize)
        => _inner.GetProductsListAsync(
                search, departmentSlug, categorySlug,
                sortBy, page, pageSize);

        public Task<List<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int count = 4)
        => _inner.GetRelatedProductsAsync(
                categoryId, excludeProductId, count);
    }
}
