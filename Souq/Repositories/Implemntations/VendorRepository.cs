using Microsoft.EntityFrameworkCore;
using Souq.Data;
using Souq.Models;
using Souq.Models.Enums;
using Souq.Repositories.Interfaces;

namespace Souq.Repositories.Implemntations
{
    public class VendorRepository : GenericRepository<VendorProfile>, IVendorRepository
    {
        public VendorRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<VendorProfile?> GetBySlugAsync(string slug)
        {
            return await _dbSet
                .Include(v => v.User)
                .Include(v => v.Products)
                    .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(v => v.StoreSlug == slug);
        }

        public async Task<VendorProfile?> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(v => v.User)
                .FirstOrDefaultAsync (v => v.UserId == userId);
        }

        public async Task<IEnumerable<VendorProfile>> GetPendingVendorAsync()
        {
            return await _dbSet
                .Where(v => v.Status == VendorStatus.Pending)
                .Include(v => v.User)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<VendorProfile?> GetVendorWithProductsAsync(int vendorId)
        {
            return await _dbSet
                .Include(v => v.Products)
                    .ThenInclude(p => p.Images)
                .Include(v => v.Products)
                    .ThenInclude(p => p.Variations)
                .Include(v => v.Products)
                    .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(v => v.Id == vendorId);
        }
    }
}
