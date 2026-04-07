using Souq.Models;
using Souq.ViewModels.Products;

namespace Souq.Services.Interfaces
{
    public interface IProductService
    {
        Task<List<Product>> GetFeaturedProductsAsync(int count=8);
        Task<List<Department>> GetAllDepartmentsAsync();
        Task<ProductDetailViewModel?> GetProductDetailAsync(string slug);
        Task<List<Product>> GetRelatedProductsAsync(int categoryId,
                                                     int excludeProductId,
                                                     int count = 4);
    }
}
