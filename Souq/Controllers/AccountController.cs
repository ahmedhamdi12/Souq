using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Souq.Models;
using Souq.UnitOfWork;
using Souq.ViewModels.Auth;

namespace Souq.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _uow;


        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUnitOfWork uow)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _uow = uow;

        }

        [HttpGet]
        public IActionResult Register() => 
                User.Identity!.IsAuthenticated ? RedirectToAction("Index", "Home") : View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null) {
                ModelState.AddModelError(string.Empty, "Email is already registered.");
                return View(model);
            }
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CreatedAt = DateTime.UtcNow
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            
            await _userManager.AddToRoleAsync(user, "Customer");

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");

        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null) {

            if (User.Identity!.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                userName: model.Email,
                password: model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            /*
                After successful login — merge guest cart into user cart.
                Read the session cookie BEFORE it gets cleared.
                Then find the user and merge their guest items.
            */
            var sessionId = Request.Cookies["souq_session_id"];
            if (!string.IsNullOrEmpty(sessionId))
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    await _uow.Cart.MergeGuestCartAsync(sessionId, user.Id);
                    await _uow.SaveAsync();

                    // Clear the guest session cookie — no longer needed
                    Response.Cookies.Delete("souq_session_id");
                }
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ForceLogout()
        {
            await _signInManager.SignOutAsync();

            // Delete all cookies
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            return RedirectToAction("Index", "Home");
        }

    } 
}
