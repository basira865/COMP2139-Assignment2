using COMP2139_Assignment1_1.Data;
using COMP2139_Assignment1_1.Models;
using COMP2139_Assignment1_1.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace COMP2139_Assignment1_1.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Dashboard
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var userId = user.Id;
            var now = DateTime.UtcNow;

            var viewModel = new DashboardViewModel
            {
                // Section 1: My Tickets (Upcoming Events)
                MyTickets = await _context.Purchases
                    .Include(p => p.PurchaseEvents)
                        .ThenInclude(pe => pe.Event)
                            .ThenInclude(e => e.Category)
                    .Where(p => p.UserId == userId)
                    .Where(p => p.PurchaseEvents.Any(pe => pe.Event.DateTime.UtcDateTime > now))
                    .OrderBy(p => p.PurchaseEvents.Min(pe => pe.Event.DateTime))
                    .ToListAsync(),

                // Section 2: Purchase History (Past Events)
                PurchaseHistory = await _context.Purchases
                    .Include(p => p.PurchaseEvents)
                        .ThenInclude(pe => pe.Event)
                            .ThenInclude(e => e.Category)
                    .Where(p => p.UserId == userId)
                    .Where(p => p.PurchaseEvents.All(pe => pe.Event.DateTime.UtcDateTime <= now))
                    .OrderByDescending(p => p.PurchaseDate)
                    .ToListAsync(),

                // Section 3: My Events (for Organizers only)
                MyEvents = User.IsInRole("Organizer") || User.IsInRole("Admin")
                    ? await _context.Events
                        .Include(e => e.Category)
                        .Include(e => e.PurchaseEvents)
                        .Where(e => e.OrganizerId == userId)
                        .OrderByDescending(e => e.DateTime)
                        .ToListAsync()
                    : new List<Event>(),

                // Section 4: User Profile
                UserProfile = user
            };

            return View(viewModel);
        }

        // GET: /Dashboard/GenerateQR?purchaseId=5
        public IActionResult GenerateQR(int purchaseId)
        {
            var purchase = _context.Purchases
                .Include(p => p.PurchaseEvents)
                    .ThenInclude(pe => pe.Event)
                .FirstOrDefault(p => p.PurchaseId == purchaseId);

            if (purchase == null)
                return NotFound();

            // Check if user owns this purchase
            var userId = _userManager.GetUserId(User);
            if (purchase.UserId != userId)
                return Forbid();

            // Generate QR code content
            var qrText = $"Ticket ID: {purchase.PurchaseId}\n" +
                        $"Guest: {purchase.GuestName}\n" +
                        $"Email: {purchase.GuestEmail}\n" +
                        $"Events: {string.Join(", ", purchase.PurchaseEvents.Select(pe => pe.Event.Title))}";

            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(20);

            return File(qrBytes, "image/png");
        }

        // POST: /Dashboard/RateEvent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateEvent(int purchaseId, int rating)
        {
            var purchase = await _context.Purchases.FindAsync(purchaseId);

            if (purchase == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (purchase.UserId != userId)
                return Forbid();

            if (rating < 1 || rating > 5)
                return BadRequest("Rating must be between 1 and 5.");

            purchase.Rating = rating;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Rating submitted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Dashboard/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, IFormFile? profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;

            // Handle profile picture upload
            if (profilePicture != null && profilePicture.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{user.Id}_{Path.GetFileName(profilePicture.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }

                user.ProfilePicturePath = $"/uploads/profiles/{uniqueFileName}";
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update profile.";
            }

            return RedirectToAction(nameof(Index));
                    }
                }
            }