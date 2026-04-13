using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Souq.Services.Interfaces;

namespace Souq.Controllers
{
    [Authorize (Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _adminService.GetDashboardAsync();
            return View(model);
        }

        // GET: /admin/vendors
        public async Task<IActionResult> Vendors()
        {
            var model = await _adminService.GetAllVendorsAsync();
            return View(model);
        }

        // POST: /admin/vendors/approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVendor(int vendorId)
        {
            await _adminService.ApproveVendorAsync(vendorId);
            TempData["SuccessMessage"] = "Vendor approved successfully.";
            return RedirectToAction("Vendors");
        }

        // POST: /admin/vendors/reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVendor(int vendorId)
        {
            await _adminService.RejectVendorAsync(vendorId);
            TempData["Success"] = "Vendor rejected successfully.";
            return RedirectToAction("Vendors");
        }

        // GET: /admin/products
        public async Task<IActionResult> Products()
        {
            var products =await _adminService.GetAllProductsAsync();
            return View(products);
        }

        // POST: /admin/products/approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProduct(int productId)
        {
            await _adminService.ApproveProductAsync(productId);
            TempData["Success"] = "Product approved successfully.";
            return RedirectToAction("Products");
        }

        // POST: /admin/products/reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProduct(int productId)
        {
            await _adminService.RejectProductAsync(productId);
            TempData["Success"] = "Product rejected successfully.";
            return RedirectToAction("Products");
        }
    }
}
