using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Souq.ViewModels.Vendor
{
    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /*
            Slug is auto-generated from name.
            We don't show it in the form —
            it's computed server-side.
        */
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        [Required]
        [Range(0.01, 999999)]
        public decimal BasePrice { get; set; }

        public bool HasVariations { get; set; }

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        /*
            Dropdown data — filled by the controller.
            Not posted back — rebuilt on validation failure.
        */
        public List<SelectListItem> Categories { get; set; } = new();

        /*
            Variations as JSON string.
            We pass variations from the form as a JSON string
            because HTML forms can't natively handle
            dynamic arrays of complex objects.
            Alpine.js builds this on the client side.
        */
        public string VariationsJson { get; set; } = "[]";

        /*
            Existing images (for edit mode).
            New images come via IFormFile uploads.
        */
        public List<ExistingImageViewModel> ExistingImages { get; set; } = new();

        /*
            Multiple file uploads.
            IFormFile is ASP.NET's type for uploaded files.
        */
        public List<IFormFile>? NewImages { get; set; }

        public bool IsEdit => Id > 0;
    }

    public class ExistingImageViewModel
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public bool ShouldDelete { get; set; }
    }

    public class VariationFormItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public bool ShouldDelete { get; set; }
    }
}
