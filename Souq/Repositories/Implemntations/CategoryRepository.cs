using Souq.Data;
using Souq.Models;
using Souq.Repositories.Interfaces;

namespace Souq.Repositories.Implemntations
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context)
        {
        }
    }
}
