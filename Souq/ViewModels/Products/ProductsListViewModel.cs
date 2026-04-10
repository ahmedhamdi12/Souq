using Souq.Models;

namespace Souq.ViewModels.Products
{
    public class ProductsListViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Department> Departments { get; set; } = new();

        // Current filters
        public string? SearchQuery { get; set; }
        public string? DepartmentSlug { get; set; }
        public string? CategorySlug { get; set; }
        public string SortBy { get; set; } = "newest";

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalProducts { get; set; }
        public int PageSize { get; set; } = 12;

        // Display helpers
        public bool HasFilters =>
            !string.IsNullOrEmpty(SearchQuery) ||
            !string.IsNullOrEmpty(DepartmentSlug) ||
            !string.IsNullOrEmpty(CategorySlug);

        public string PageTitle =>
            !string.IsNullOrEmpty(SearchQuery)
                ? $"Results for \"{SearchQuery}\""
                : !string.IsNullOrEmpty(DepartmentSlug)
                    ? Departments.FirstOrDefault(d =>
                        d.Slug == DepartmentSlug)?.Name ?? "Products"
                    : "All products";
    }
}
