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
        private readonly IUnitOfWork _uow;

        public VendorController(IOrderService orderService, IUnitOfWork uow)
        {
            _orderService = orderService;
            _uow = uow;
        }

        // get vendor profile 
        private async Task<int> GetVendorIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var vendor = await _uow.Vendors.GetByUserIdAsync(userId);
            return vendor?.Id ?? 0;
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
