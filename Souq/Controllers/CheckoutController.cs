using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Souq.Services.Interfaces;
using Souq.ViewModels.Checkout;

namespace Souq.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;

        public CheckoutController(
            IOrderService orderService,
            ICartService cartService)
        {
            _orderService = orderService;
            _cartService = cartService;
        }

        // ── GET: /checkout ───────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var cart = await _cartService.GetCartAsync(userId, null);

            if (cart.IsEmpty)
                return RedirectToAction("Index", "Cart");

            var model = new CheckoutViewModel { Cart = cart };
            return View(model);
        }

        // ── POST: /checkout ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var cart = await _cartService.GetCartAsync(userId, null);

            if (cart.IsEmpty)
                return RedirectToAction("Index", "Cart");

            // Re-attach cart to model (not posted back in form)
            model.Cart = cart;

            if (!ModelState.IsValid)
                return View(model);

            /*
                Create the order in our database first (Draft status).
                Then create Stripe session and redirect.
                If Stripe fails, order exists but stays Draft.
                Webhook will update it to Paid when payment succeeds.
            */
            var order = await _orderService
                .CreateOrderAsync(model, userId);


            var successUrl = Url.Action("Success", "Checkout",
                null, Request.Scheme)!;
            var cancelUrl = Url.Action("Cancel", "Checkout",
                null, Request.Scheme)!;


            var stripeUrl = await _orderService
                .CreateStripeSessionAsync(order, successUrl, cancelUrl);

            // Redirect to Stripe hosted checkout page
            return Redirect(stripeUrl);
        }

        // ── GET: /checkout/success ───────────────────────────
        public async Task<IActionResult> Success(int orderId)
        {
            var model = await _orderService.GetOrderSuccessAsync(orderId);
            if (model == null) return NotFound();

            return View(model);
        }

        // ── GET: /checkout/cancel ────────────────────────────
        public IActionResult Cancel()
        {
            return View();
        }

        // ── GET: /checkout/check-payment (AJAX polling) ──────
        [HttpGet]
        public async Task<IActionResult> CheckPayment(int orderId)
        {
            /*
                The success page polls this endpoint every 2 seconds.
                Once the webhook fires and marks the order as Paid,
                this returns isPaid: true and the page shows ✅
            */
            var isPaid = await _orderService.IsOrderPaidAsync(orderId);
            return Json(new { isPaid });
        }

        // ── POST: /checkout/webhook ──────────────────────────
        /*
            This endpoint is called by Stripe directly.
            It must NOT have [Authorize] or [ValidateAntiForgeryToken]
            because Stripe doesn't send auth cookies or CSRF tokens.
            We verify the request using Stripe's webhook signature instead.
        */
        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body)
                .ReadToEndAsync();

            var stripeSignature = Request.Headers["Stripe-Signature"];

            try
            {
                await _orderService.HandleWebhookAsync(json, stripeSignature!);
                return Ok();
            }
            catch (InvalidOperationException)
            {
                return BadRequest();
            }
        }
    }
}
