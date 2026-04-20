using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Souq.Models;
using Souq.Services.Implementations;
using Souq.Services.Interfaces;
using Souq.UnitOfWork;
using Souq.ViewModels.Auth;

namespace Souq.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _uow;


        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUnitOfWork uow, IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _uow = uow;
            _emailService = emailService;
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
                FirstName = InputSanitizer.Sanitize(model.FirstName),
                LastName = InputSanitizer.Sanitize(model.LastName),
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
            /*
        Generate email confirmation token.
        This token is unique to this user and expires.
        We embed it in the verification URL.
    */
            var token = await _userManager
                .GenerateEmailConfirmationTokenAsync(user);

            /*
                Encode the token for URL safety.
                Tokens contain special characters (+, /, =)
                that would break the URL if not encoded.
            */
            var encodedToken = System.Net.WebUtility.UrlEncode(token);

            var verificationUrl = Url.Action(
                "ConfirmEmail", "Account",
                new { userId = user.Id, token = encodedToken },
                Request.Scheme)!;

            /*
                Send verification email.
                Wrapped in try-catch so email failure
                doesn't prevent account creation.
            */
            try
            {
                await _emailService.SendEmailVerificationAsync(
                    toEmail: user.Email!,
                    firstName: user.FirstName,
                    verificationUrl: verificationUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Verification email failed: {ex.Message}");
            }

            /*
                Don't sign in automatically anymore.
                Redirect to a "check your email" page.
            */
            return View("RegisterConfirmation",
                model: (object)user.Email!);

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
                /*
                    Check specifically if account is not confirmed.
                    Give a clear message instead of generic "invalid".
                */
                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty,
                        "Please verify your email address before signing in. " +
                        "Check your inbox for the verification link.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty,
                        "Invalid email or password.");
                }
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

        // ── GET: /account/confirm-email ──────────────────────────
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(
            string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return View("ConfirmEmailError");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return View("ConfirmEmailError");

            /*
                Decode the token back before verifying.
                We encoded it when generating the URL.
            */
            var decodedToken = System.Net.WebUtility.UrlDecode(token);

            var result = await _userManager
                .ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
                return View("ConfirmEmailError");

            /*
                Auto sign in after confirming email —
                good UX, no need to go to login page.
            */
            await _signInManager.SignInAsync(user, isPersistent: false);

            return View("ConfirmEmailSuccess");
        }

        // ── GET: /account/forgot-password ────────────────────────
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        // ── POST: /account/forgot-password ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            /*
                IMPORTANT: Always show success even if email not found.
                This prevents user enumeration attacks where
                an attacker discovers which emails are registered
                by checking the error message.
            */
            if (user == null || !await _userManager
                .IsEmailConfirmedAsync(user))
            {
                return View("ForgotPasswordConfirmation");
            }

            var token = await _userManager
                .GeneratePasswordResetTokenAsync(user);

            var encodedToken = System.Net.WebUtility.UrlEncode(token);

            var resetUrl = Url.Action(
                "ResetPassword", "Account",
                new { userId = user.Id, token = encodedToken },
                Request.Scheme)!;

            try
            {
                await _emailService.SendPasswordResetAsync(
                    toEmail: user.Email!,
                    firstName: user.FirstName,
                    resetUrl: resetUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Password reset email failed: {ex.Message}");
            }

            return View("ForgotPasswordConfirmation");
        }

        // ── GET: /account/reset-password ─────────────────────────
        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            return View(new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            });
        }

        // ── POST: /account/reset-password ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return View("ResetPasswordConfirmation");

            var decodedToken = System.Net.WebUtility.UrlDecode(model.Token);

            var result = await _userManager.ResetPasswordAsync(
                user, decodedToken, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            return View("ResetPasswordConfirmation");
        }

    } 
}
