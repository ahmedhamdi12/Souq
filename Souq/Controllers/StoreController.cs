using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Souq.Services.Interfaces;

namespace Souq.Controllers
{
    public class StoreController : Controller
    {
        private readonly IVendorService _vendorService;

        public StoreController(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }
        public async Task<IActionResult> Index(string slug)
        {
            var model = await _vendorService.GetStoreAsync(slug);

            if (model == null)
            {
                return NotFound();
            }
            return View(model);
        }
    }
}
