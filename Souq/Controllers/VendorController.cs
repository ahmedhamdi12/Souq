using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Souq.Services.Interfaces;
using Souq.UnitOfWork;
using Souq.ViewModels.Orders;

namespace Souq.Controllers
{
    [Authorize(Roles = "Vendor")]
    public class VendorController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IVendorService _vendorService;
        private readonly IUnitOfWork _uow;
        private readonly IWebHostEnvironment _env;

        public VendorController(IOrderService orderService, IVendorService vendorService, IUnitOfWork uow, IWebHostEnvironment env)
        {
            _orderService = orderService;
            _vendorService = vendorService;
            _uow = uow;
            _env = env;
        }

        // get vendor profile 
        private async Task<int> GetVendorIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var vendor = await _uow.Vendors.GetByUserIdAsync(userId);
            return vendor?.Id ?? 0;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ── GET: /vendor/dashboard
        public async Task<IActionResult> Dashboard()
        {
            var model = await _vendorService.GetDashboardAsync(GetUserId());
            if(model == null) return RedirectToAction("Index", "Home");
            return View(model);
        }

        // ── GET: /vendor/products
        public async Task<IActionResult> Products()
        {
            var vendorId = await GetVendorIdAsync();
            if(vendorId == 0) return RedirectToAction("Index", "Home");
            var products = await _vendorService.GetVendorProductsAsync(vendorId);
            return View(products);
        }

        // ── GET: /vendor/products/create
        public async Task<IActionResult> CreateProduct()
        {
            var vendorId = await GetVendorIdAsync();
            var model = await _vendorService.GetProductsFormAsync(null, vendorId);
            return View("ProductForm", model);
        }

        // ── GET: /vendor/products/edit/{id}
        public async Task<IActionResult> EditProduct(int id)
        {
            var vendorId = await GetVendorIdAsync();
            var model = await _vendorService.GetProductsFormAsync(id, vendorId);
            return View("ProductForm", model);
        }

        // ── POST: /vendor/products/save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProduct(Souq.ViewModels.Vendor.ProductFormViewModel model)
        {
            var vendorId = await GetVendorIdAsync();
            if (!ModelState.IsValid)
            {
                // Rebuild categories dropdown
                var refreshed = await _vendorService.GetProductsFormAsync(model.Id, vendorId);
                model.Categories = refreshed.Categories;
                return View("ProductForm", model);
            }
            var success = await _vendorService.SaveProductsAsync(model, vendorId, _env);

            if (!success) 
            { 
                ModelState.AddModelError("", "An error occurred while saving the product. Please try again.");
                var refreshed = await _vendorService.GetProductsFormAsync(model.Id, vendorId);
                model.Categories = refreshed.Categories;
                return View("ProductForm", model);
            }

            TempData["success"] = model.IsEdit
                ? "Product updated successfully!" : "Product created successfully! Waiting for Admin approval.";

            return RedirectToAction("Products");
        }

        // ── POST: /vendor/products/delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var vendorId = await GetVendorIdAsync();
            await _vendorService.DeleteProductsAsync(id, vendorId);

            TempData["success"] = "Product deleted successfully!";
            
            return RedirectToAction("Products");
        }
        public async Task<IActionResult> Orders()
        {
            var vendorId = await GetVendorIdAsync();
            if(vendorId == 0) return RedirectToAction("Index", "Home");

            var orders = await _orderService.GetVendorOrdersAsync(vendorId);

            return View(orders);
        }

        public async Task<IActionResult> OrderDetail(int id)
        {
            var vendorId = await GetVendorIdAsync();
            if(vendorId == 0) return RedirectToAction("Index", "Home");

            var order = await _orderService.GetVendorOrderDetailAsync(id, vendorId);
            if(order == null) return NotFound();

            return View(order);
        }
        public class UpdateStatusRequest
        {
            public int OrderId { get; set; }
            public string NewStatus { get; set; } = string.Empty;
        }

        [HttpPost]
        [Route("vendor/orders/update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            var vendorId = await GetVendorIdAsync();
            if (vendorId == 0)
                return Json(new { success = false });
            if(!Enum.TryParse<Souq.Models.Enums.OrderStatus>(request.NewStatus, out var status))
                return Json(new { success = false , message = "Invalid status" });
            
            var result = await _orderService.UpdateOrderStatusAsync(request.OrderId, vendorId, status);
            return Json(new { success = result });
        }
    }
}
