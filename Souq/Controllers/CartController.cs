using Microsoft.AspNetCore.Mvc;
using Souq.Services.Interfaces;

namespace Souq.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private const string SESSION_COOKIE_KEY = "souq_session_id";

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // ── GET: /cart ───────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var sessionId = Request.Cookies["souq_session_id"];
            var isAuth = User.Identity!.IsAuthenticated;

            System.Diagnostics.Debug.WriteLine(
                $"=== CART === isAuth:{isAuth} | cookieValue:{sessionId}");

            var (userId, sid) = GetCartIdentity();

            System.Diagnostics.Debug.WriteLine(
                $"=== IDENTITY === userId:{userId} | sessionId:{sid}");

            var cart = await _cartService.GetCartAsync(userId, sid);

            System.Diagnostics.Debug.WriteLine(
                $"=== ITEMS === count:{cart.Items.Count}");

            return View(cart);
        }

        // ── GET: /cart/count ─────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var (userId, sessionId) = GetCartIdentity();
            var count = await _cartService.GetCartCountAsync(userId, sessionId);
            return Json(new { count });
        }

        // ── POST: /cart/add ──────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddToCartRequest request)
        {
            if (request.VariationId <= 0)
                return Json(new { success = false, message = "Please select a variation" });

            /*
                Get or create session BEFORE doing anything else.
                This ensures the cookie is set on the response
                that returns to the browser.
            */
            var (userId, sessionId) = GetOrCreateCartIdentity();

            var newCount = await _cartService
                .AddToCartAsync(request.VariationId, request.Quantity,
                                userId, sessionId);

            return Json(new
            {
                success = true,
                cartCount = newCount,
                sessionId = sessionId  // send back so JS can verify
            });
        }

        // ── POST: /cart/update ───────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateCartRequest request)
        {
            var (userId, sessionId) = GetCartIdentity();
            var newCount = await _cartService
                .UpdateQuantityAsync(request.CartItemId, request.Quantity,
                                     userId, sessionId);

            var cart = await _cartService.GetCartAsync(userId, sessionId);

            return Json(new
            {
                success = true,
                cartCount = newCount,
                subTotal = cart.Subtotal.ToString("0.00"),
                lineTotal = cart.Items
                    .FirstOrDefault(i => i.CartItemId == request.CartItemId)
                    ?.LineTotal.ToString("0.00") ?? "0.00"
            });
        }

        // ── POST: /cart/remove ───────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Remove([FromBody] RemoveCartRequest request)
        {
            var (userId, sessionId) = GetCartIdentity();
            await _cartService
                .RemoveFromCartAsync(request.CartItemId, userId, sessionId);
            var newCount = await _cartService
                .GetCartCountAsync(userId, sessionId);
            var cart = await _cartService.GetCartAsync(userId, sessionId);

            return Json(new
            {
                success = true,
                cartCount = newCount,
                subTotal = cart.Subtotal.ToString("0.00"),
                isEmpty = cart.IsEmpty
            });
        }

        // ── Helper: get existing identity ────────────────────
        private (string? userId, string? sessionId) GetCartIdentity()
        {
            /*
        Double check — IsAuthenticated must be true AND
        the user must actually exist in the claim.
        This prevents stale cookie issues.
    */
            if (User.Identity!.IsAuthenticated)
            {
                var userId = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                // Only use userId if we actually got a value
                if (!string.IsNullOrEmpty(userId))
                    return (userId, null);
            }

            var sessionId = Request.Cookies["souq_session_id"];
            return (null, sessionId);
        }

        // ── Helper: get OR CREATE identity ───────────────────
        private (string? userId, string? sessionId) GetOrCreateCartIdentity()
        {
            /*
                If logged in → use userId, no cookie needed.
                If guest → try to read existing cookie first.
                If no cookie exists → create one now.

                Key difference from GetCartIdentity:
                this method CREATES a cookie if missing.
                GetCartIdentity only READS.
            */
            if (User.Identity!.IsAuthenticated)
            {
                var userId = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                return (userId, null);
            }

            // Try to get existing session cookie
            var existingSession = Request.Cookies[SESSION_COOKIE_KEY];
            if (existingSession != null)
                return (null, existingSession);

            // No cookie → create new one
            var newSessionId = Guid.NewGuid().ToString();

            Response.Cookies.Append(SESSION_COOKIE_KEY, newSessionId,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    /*
                        IsEssential = true tells ASP.NET this cookie
                        is required for the app to function.
                        Without this, GDPR cookie consent middleware
                        might block it from being set.
                        This is the most common reason cookies
                        don't get saved to the browser.
                    */
                    IsEssential = true
                });

            return (null, newSessionId);
        }
    }

    public class AddToCartRequest
    {
        public int VariationId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartRequest
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveCartRequest
    {
        public int CartItemId { get; set; }
    }
}