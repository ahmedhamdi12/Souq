using Souq.Models;
using Souq.ViewModels.Products;

namespace Souq.Services.Interfaces
{
    public interface IProductService
    {
        Task<List<Product>> GetFeaturedProductsAsync(int count=8);
        Task<List<Department>> GetAllDepartmentsAsync();
        Task<ProductsListViewModel> GetProductsListAsync(
                                                            string? search,
                                                            string? departmentSlug,
                                                            string? categorySlug,
                                                            string sortBy,
                                                            int page,
                                                            int pageSize);
        Task<ProductDetailViewModel?> GetProductDetailAsync(string slug);
        Task<List<Product>> GetRelatedProductsAsync(int categoryId,
                                                     int excludeProductId,
                                                     int count = 4);
        Task<bool> CanUserReviewAsync(int productId, string userId);
        Task<bool> AddReviewAsync(ReviewViewModel model, string userId);
    }
}
