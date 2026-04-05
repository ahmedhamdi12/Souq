using Souq.Models;
using Souq.Services.Interfaces;
using Souq.UnitOfWork;

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
    }
}
