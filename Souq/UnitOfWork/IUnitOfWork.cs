using Souq.Repositories.Interfaces;

namespace Souq.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Products { get; }
        
        IOrderRepository Orders { get; }
        ICartRepository Cart { get; }
        IVendorRepository Vendors { get; }
        IDepartmentRepository Departments { get; }
        Task<int> SaveAsync();
    }
}
