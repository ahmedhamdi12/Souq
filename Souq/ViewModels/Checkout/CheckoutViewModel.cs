using System.ComponentModel.DataAnnotations;
using Souq.ViewModels.Cart;

namespace Souq.ViewModels.Checkout
{
    public class CheckoutViewModel
    {
        /*
            The checkout page shows:
            1. Order summary (from cart)
            2. Shipping address form
            User fills shipping, we create Stripe session, redirect.
        */
        public CartViewModel Cart { get; set; } = new();

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        [Phone]
        public string Phone { get; set; } = string.Empty;
    }
}