using Souq.Models;

namespace Souq.Repositories.Interfaces
{
    public interface IVariationRepository :IGenericRepository<ProductVariation>
    {
        Task<bool> HasOrderItemsAsync(int productId);

    }

}
