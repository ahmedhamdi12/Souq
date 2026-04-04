using Souq.Models;

namespace Souq.Repositories.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetBySlugAsync(string slug);
         Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId);
        Task<IEnumerable<Product>> GetApprovedProductAsync();
         Task<IEnumerable<Product>> GetProductsByVendorIdAsync(int vendorId);
        Task<IEnumerable<Product>> SearchProductsAsync(string query);
        Task<Product?> GetProductWithDetailsAsync(int id);
    }
}
