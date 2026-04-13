
using Souq.ViewModels.Vendor;

namespace Souq.Services.Interfaces
{
    public interface IVendorService
    {
        Task<VendorDashboardViewModel?> GetDashboardAsync(string userId);
        Task<List<ProductListItemViewModel>> GetVendorProductsAsync(int vendorId);
        Task<ProductFormViewModel> GetProductsFormAsync(int? productId, int vendorId);
        Task<bool> SaveProductsAsync(ProductFormViewModel model, int vendorId, IWebHostEnvironment env);
        Task<bool> DeleteProductsAsync(int productId, int vendorId);
        Task<bool> HasExistingApplicationAsync(string userId);
        Task<bool> ApplyAsVendorAsync(
            VendorApplicationViewModel model, string userId);
    }
}
