namespace Souq.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int SortOrder { get; set; } 
        // Navigation properties
        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
