using System.ComponentModel.DataAnnotations;

namespace Souq.ViewModels.Vendor
{
    public class VendorApplicationViewModel
    {
        [Required(ErrorMessage = "Store name is required")]
        [MinLength(3, ErrorMessage = "Store name must be at least 3 characters")]
        [MaxLength(100)]
        [Display(Name = "Store Name")]
        public string StoreName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [MinLength(20, ErrorMessage = "Please write at least 20 characters")]
        [MaxLength(500)]
        [Display(Name = "Store Description")]
        public string Description { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Business Phone")]
        public string? Phone { get; set; }

        public IFormFile? LogoFile { get; set; }
    }
}
