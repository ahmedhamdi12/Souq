using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Souq.Services.Interfaces;
using Souq.ViewModels.Products;

namespace Souq.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }
        // GET: /products
        public async Task<IActionResult> Index(
            string? q = null,
            string? dept = null,
            string? category = null,
            string sort = "newest",
            int page = 1)
        {
            var model = await _productService.GetProductsListAsync(
                search: q,
                departmentSlug: dept,
                categorySlug: category,
                sortBy: sort,
                page: page,
                pageSize: 12);

            return View(model);
        }


        // GET: /products/department/{slug}
        public async Task<IActionResult> Department(
            string slug, string sort = "newest", int page = 1)
        {
            var model = await _productService.GetProductsListAsync(
                search: null,
                departmentSlug: slug,
                categorySlug: null,
                sortBy: sort,
                page: page,
                pageSize: 12);

            return View("Index", model);
        }

        // GET: /products/search?q=...
        public async Task<IActionResult> Search(
            string q, string sort = "newest", int page = 1)
        {
            var model = await _productService.GetProductsListAsync(
                search: q,
                departmentSlug: null,
                categorySlug: null,
                sortBy: sort,
                page: page,
                pageSize: 12);

            return View("Index", model);
        }


        // GET: /products/{slug}
        public async Task<IActionResult> Details(string slug)
        {
            var viewModel = await _productService
                .GetProductDetailAsync(slug);

            if (viewModel == null)
                return NotFound();

            ViewData["VariationsJson"] =
                System.Text.Json.JsonSerializer.Serialize(
                    viewModel.Product.Variations.ToDictionary(
                        v => v.Id,
                        v => new
                        {
                            id = v.Id,
                            price = v.Price,
                            stock = v.AvailableStock,
                            color = v.Color ?? "",
                            size = v.Size ?? "",
                            image = v.ImageUrl ?? ""
                        }));

            ViewData["ImagesByColorJson"] =
                System.Text.Json.JsonSerializer.Serialize(
                    viewModel.ImagesByColor);

            ViewData["AllImagesJson"] =
                System.Text.Json.JsonSerializer.Serialize(
                    viewModel.AllImages);

            /*
                Calculate CanReview in the controller — not the view.
                Pass result to view via ViewData.
                ViewData["CanReview"] = true/false
            */
            if (User.Identity!.IsAuthenticated)
            {
                var userId = User.FindFirstValue(
                    System.Security.Claims.ClaimTypes.NameIdentifier)!;

                ViewData["CanReview"] = await _productService
                    .CanUserReviewAsync(viewModel.Product.Id, userId);
            }
            else
            {
                ViewData["CanReview"] = false;
            }

            return View(viewModel);
        }


        // POST: /products/review
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(ReviewViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Details",
                    new { slug = model.ProductSlug });

            var userId = User.FindFirstValue(
                System.Security.Claims.ClaimTypes.NameIdentifier)!;

            var success = await _productService
                .AddReviewAsync(model, userId);

            TempData[success ? "ReviewSuccess" : "ReviewError"] =
                success
                    ? "Thank you for your review!"
                    : "You can only review products you have purchased.";

            return RedirectToAction("Details",
                new { slug = model.ProductSlug });
        }
    }
}
