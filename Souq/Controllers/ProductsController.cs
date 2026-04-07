using Microsoft.AspNetCore.Mvc;
using Souq.Services.Interfaces;

namespace Souq.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetFeaturedProductsAsync(20);
            return View(products);
        }

        // GET: /products/{slug}
        public async Task<IActionResult> Details(string slug)
        {
            var viewModel = await _productService
                .GetProductDetailAsync(slug);

            if (viewModel == null)
                return NotFound();

            return View(viewModel);
        }
    }
}
