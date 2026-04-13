using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Souq.Services.Interfaces;
using Souq.ViewModels.Vendor;

namespace Souq.Controllers
{
    public class VendorApplicationController : Controller
    {
        private readonly IVendorService _vendorService;

        public VendorApplicationController(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        // GET: /vendor/apply
        [HttpGet]
        public async Task<IActionResult> Apply()
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login", "Account",
                    new { returnUrl = "/vendor/apply" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (User.IsInRole("Vendor"))
                return RedirectToAction("Dashboard", "Vendor");

            var hasApplied = await _vendorService
                .HasExistingApplicationAsync(userId);
            
            if (hasApplied)
                return View("AlreadyApplied");
            return View(new VendorApplicationViewModel());
        }

        // POST: /vendor/apply
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(VendorApplicationViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Check if already applied
            if (await _vendorService.HasExistingApplicationAsync(userId))
                return View("AlreadyApplied");

            if (!ModelState.IsValid)
                return View(model);

            var success = await _vendorService
                .ApplyAsVendorAsync(model, userId);

            if (!success)
            {
                ModelState.AddModelError("",
                    "Something went wrong. Please try again.");
                return View(model);
            }

            return View("ApplicationSubmitted");
        }
    }
}
