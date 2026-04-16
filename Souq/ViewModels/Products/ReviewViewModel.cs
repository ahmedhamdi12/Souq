using System.ComponentModel.DataAnnotations;

namespace Souq.ViewModels.Products
{
    public class ReviewViewModel
    {
        public int ProductId { get; set; }
        public string ProductSlug { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Please select a rating")]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

    }
}
