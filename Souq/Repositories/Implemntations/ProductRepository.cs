using Microsoft.EntityFrameworkCore;
using Souq.Data;
using Souq.Models;
using Souq.Repositories.Interfaces;

namespace Souq.Repositories.Implemntations
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context)
        {
        }

        public new async Task<IEnumerable<Product>> GetAllAsync() =>
        await _context.Products
        .Include(p => p.Vendor)
        .Include(p => p.Images)
        .Include(p => p.Variations)
        .Include(p => p.Category)
            .ThenInclude(c => c.Department)
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();

        public async Task<IEnumerable<Product>> GetApprovedProductAsync()
        {
            return await _dbSet.Where(p => p.IsApproved && p.IsActive)
                .Include(p => p.Vendor)
                .Include(p => p.Images)
                .Include(p => p.Variations)
                .Include(p => p.Category)
                    .ThenInclude(c => c.Department)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Product?> GetBySlugAsync(string slug)
        {
            return await _dbSet.Include(p => p.Vendor)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Slug == slug);
        }
        public async Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId)
        {
            return await _dbSet.Where(p => p.CategoryId == categoryId && p.IsActive && p.IsApproved)
                .Include(p => p.Vendor)
                .Include(p => p.Images)
                .Include(p => p.Variations)
                .Include(p => p.Category)
                    .ThenInclude(c => c.Department)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByVendorIdAsync(int vendorId)
        {
            return await _dbSet.Where(p => p.VendorId == vendorId)
                .Include(p => p.Category)
                        .ThenInclude(c => c.Department)
                .Include(p => p.Images)
                .Include(p => p.Variations)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Product?> GetProductWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Vendor)
                .Include(p => p.Category)
                    .ThenInclude(c => c.Department)
                .Include(p => p.Images)
                .Include(p => p.Variations)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string query)
        { 
              return await _dbSet.
              Where(p => p.IsApproved && p.IsActive &&
             ( p.Name.Contains(query) || p.Description!.Contains(query)))
                .Include(p => p.Vendor)
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
    }
        
    }
    }
