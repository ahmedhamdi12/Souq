using Souq.ViewModels.Admin;

namespace Souq.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardAsync();
        Task<List<AdminVendorViewModel>> GetAllVendorsAsync();
        Task<bool> ApproveVendorAsync(int vendorId);
        Task<bool> RejectVendorAsync(int vendorId);
        Task<List<AdminProductViewModel>> GetAllProductsAsync();
        Task<bool> ApproveProductAsync(int productId);
        Task<bool> RejectProductAsync(int productId);
    }
}
