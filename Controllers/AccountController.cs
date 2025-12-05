using COMP2139_Assignment1_1.Models;
using COMP2139_Assignment1_1.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;

namespace COMP2139_Assignment1_1.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Assign role
                var role = model.IsOrganizer ? "Organizer" : "Attendee";
                await _userManager.AddToRoleAsync(user, role);
                
                // ✅ Log registration
                _logger.LogInformation("New user registered: {Email} as {Role}", model.Email, role);

                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                
                var confirmUrl = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new { userId = user.Id, token = encodedToken },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    model.Email,
                    "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(confirmUrl)}'>clicking here</a>.");

                // ✅ Add success message for registration confirmation page
                TempData["SuccessMessage"] = "Registration successful! Please check your email to confirm your account.";

                return RedirectToAction(nameof(RegisterConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: /Account/RegisterConfirmation
        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded)
            {
                // ✅ Log email confirmation
                _logger.LogInformation("Email confirmed for user: {Email}", user.Email);
                
                TempData["SuccessMessage"] = "Email confirmed! You can now log in.";
                return RedirectToAction(nameof(Login));
            }

            return View("Error");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // ✅ Log successful login
                _logger.LogInformation("User {Email} logged in successfully from IP {IP}", 
                    model.Email, 
                    HttpContext.Connection.RemoteIpAddress);

                // ✅ Add welcome message
                TempData["SuccessMessage"] = $"Welcome back, {model.Email}!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
        
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
            }

            if (result.IsLockedOut)
            {
                // ✅ Log account lockout
                _logger.LogWarning("User {Email} account locked out from IP {IP}", 
                    model.Email, 
                    HttpContext.Connection.RemoteIpAddress);

                ModelState.AddModelError(string.Empty, "Account locked due to multiple failed login attempts.");
                return View(model);
            }

            // ✅ Log failed login
            _logger.LogWarning("Failed login attempt for {Email} from IP {IP}", 
                model.Email, 
                HttpContext.Connection.RemoteIpAddress);

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // GET: /Account/LoginWith2fa (placeholder)
        [HttpGet]
        public IActionResult LoginWith2fa(string? returnUrl = null, bool rememberMe = false)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userEmail = User.Identity?.Name ?? "Unknown";
    
            await _signInManager.SignOutAsync();
    
            // ✅ Log logout
            _logger.LogInformation("User {Email} logged out", userEmail);
    
            // ✅ Add logout success message
            TempData["SuccessMessage"] = "You have been logged out successfully.";
    
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            // ✅ Log access denied
            _logger.LogWarning("Access denied for user {User}. Attempted URL: {ReturnUrl}", 
                User.Identity?.Name ?? "Anonymous", 
                returnUrl ?? "Unknown");
    
            return View();
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email is required.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            
            var callbackUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { token = encodedToken },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                email,
                "Reset Password",
                $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            // ✅ Log password reset request
            _logger.LogInformation("Password reset requested for user: {Email}", email);

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string? token = null)
        {
            if (token == null)
                return BadRequest("A token must be supplied for password reset.");

            return View();
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string token, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, password);

            if (result.Succeeded)
            {
                // ✅ Log password reset success
                _logger.LogInformation("Password successfully reset for user: {Email}", email);
                
                TempData["SuccessMessage"] = "Password reset successful! You can now log in.";
                
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View();
        }

        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}

