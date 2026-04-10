using Souq.Data;
using Souq.Models;
using Souq.Repositories.Interfaces;

namespace Souq.Repositories.Implemntations
{
    public class VariationRepository : GenericRepository<ProductVariation>,
                                       IVariationRepository
    {
        public VariationRepository(AppDbContext context) : base(context) { }
    }
    
}
