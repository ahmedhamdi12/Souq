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

        public UnitOfWork(AppDbContext context, IProductRepository products, IOrderRepository orders, ICartRepository cart, IVendorRepository vendors)
        {
            _context = context;
            Products = products;
            Orders = orders;
            Cart = cart;
            Vendors = vendors;
        }

        public void Dispose() => _context.Dispose();


        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();

    }
}
