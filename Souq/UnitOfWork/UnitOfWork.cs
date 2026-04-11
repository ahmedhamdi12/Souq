using Souq.Data;
using Souq.Repositories.Interfaces;

namespace Souq.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public IProductRepository Products { get; }

        public IOrderRepository Orders { get; }

        public ICartRepository Cart { get; }

        public IVendorRepository Vendors { get; }
        public IDepartmentRepository Departments { get; }
        public IVariationRepository Variations { get; }
        public ICategoryRepository Categories { get; }
        public IProductImageRepository ProductImages { get; }
        public IReviewRepository Reviews { get; }

        public UnitOfWork(AppDbContext context, 
            IProductRepository products, 
            IOrderRepository orders, 
            ICartRepository cart, 
            IVendorRepository vendors, 
            IDepartmentRepository departments, 
            IVariationRepository variations, 
            ICategoryRepository categories, 
            IProductImageRepository productImages, 
            IReviewRepository reviews)
        {
            _context = context;
            Products = products;
            Orders = orders;
            Cart = cart;
            Variations = variations;
            Vendors = vendors;
            Departments = departments;
            Categories = categories;
            ProductImages = productImages;
            Reviews = reviews;
        }

        public void Dispose() => _context.Dispose();


        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();

    }
}
