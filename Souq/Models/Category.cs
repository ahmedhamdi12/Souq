namespace Souq.Models
{
    public class Category
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int SortOrder { get; set; }

        // Navigation properties
        public Department Department { get; set; } = null!;
        public ICollection<Product> Products { get; set; } = new List<Product>();

    }
}
