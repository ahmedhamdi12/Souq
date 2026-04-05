using Souq.Models;

namespace Souq.ViewModels.Home
{
    public class HomeViewModel
    {
        public List<Department> Departments { get; set; } = new ();
        public List<Product> FeaturedProducts { get; set; } = new ();
    }
}
