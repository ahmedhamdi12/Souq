using Microsoft.EntityFrameworkCore;
using Souq.Data;
using Souq.Models;
using Souq.Repositories.Interfaces;

namespace Souq.Repositories.Implemntations
{
    public class VariationRepository : GenericRepository<ProductVariation>,
                                       IVariationRepository
    {
        private readonly AppDbContext _context;
        public VariationRepository(AppDbContext context) : base(context) 
        {
            _context = context;
        }

        public async Task<bool> HasOrderItemsAsync(int productId) =>
        
            await _context.OrderItems.AnyAsync(oi => oi.Variation.ProductId == productId);
             
        
    }
    
}
