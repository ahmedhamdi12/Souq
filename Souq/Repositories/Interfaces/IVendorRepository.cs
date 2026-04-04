using Souq.Models;

namespace Souq.Repositories.Interfaces
{
    public interface IVendorRepository : IGenericRepository<VendorProfile>
    {
            Task<VendorProfile?> GetByUserIdAsync(string userId);
            Task<VendorProfile?> GetBySlugAsync(string slug);
            Task<IEnumerable<VendorProfile>> GetPendingVendorAsync();
            Task<VendorProfile?> GetVendorWithProductsAsync(int vendorId);

    }
}
