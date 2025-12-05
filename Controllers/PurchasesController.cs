using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COMP2139_Assignment1_1.Data;
using COMP2139_Assignment1_1.Models;
using COMP2139_Assignment1_1.Helpers;

namespace COMP2139_Assignment1_1.Controllers
{
    public class PurchasesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PurchasesController> _logger; // ✅ ADD

        public PurchasesController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<PurchasesController> logger) // ✅ ADD
        {
            _context = context;
            _userManager = userManager;
            _logger = logger; // ✅ ADD
        }

        public IActionResult Index()
        {
            return RedirectToAction("Create");
        }

        // GET: /Purchases/Create
        public async Task<IActionResult> Create()
        {
            var cartItems = CartHelper.GetCart(HttpContext.Session);
            var availableEvents = await _context.Events
                .Include(e => e.Category)
                .Where(e => e.AvailableTickets > 0)
                .ToListAsync();

            var viewModel = new CheckoutViewModel
            {
                CartItems = cartItems,
                AvailableEvents = availableEvents
            };

            return View(viewModel);
        }

        // POST: /Purchases/Confirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(string GuestName, string GuestEmail)
        {
            var cartItems = CartHelper.GetCart(HttpContext.Session);
            if (!cartItems.Any())
            {
                _logger.LogWarning("Purchase attempt with empty cart"); // ✅ ADD
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Create");
            }

            var purchase = new Purchase
            {
                GuestName = string.IsNullOrWhiteSpace(GuestName) ? "Anonymous" : GuestName,
                GuestEmail = string.IsNullOrWhiteSpace(GuestEmail) ? "guest@example.com" : GuestEmail,
                PurchaseDate = DateTime.UtcNow,
                TotalCost = cartItems.Sum(i => i.Subtotal)
            };

            // Link to authenticated user if logged in
            if (User.Identity.IsAuthenticated)
            {
                purchase.UserId = _userManager.GetUserId(User);
                
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    purchase.GuestName = user.FullName ?? user.Email;
                    purchase.GuestEmail = user.Email;
                }
            }

            foreach (var item in cartItems)
            {
                var ev = await _context.Events.FindAsync(item.EventId);
                if (ev == null || ev.AvailableTickets < item.Quantity)
                {
                    _logger.LogWarning("Insufficient tickets for Event {EventId}", item.EventId); // ✅ ADD
                    TempData["ErrorMessage"] = $"Not enough tickets for {item.EventTitle}.";
                    return RedirectToAction("Create");
                }

                ev.AvailableTickets -= item.Quantity;

                purchase.PurchaseEvents.Add(new PurchaseEvent
                {
                    EventId = item.EventId,
                    Quantity = item.Quantity,
                    TotalPrice = item.Subtotal
                });
            }

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();
            
            // ✅ PART 4: Log purchase
            _logger.LogInformation("Purchase {PurchaseId} completed by {User}. Total: ${Total}", 
                purchase.PurchaseId, 
                User.Identity?.Name ?? purchase.GuestEmail, 
                purchase.TotalCost);
            
            CartHelper.ClearCart(HttpContext.Session);
            
            // ✅ PART 3.3: Trigger modal
            TempData["PurchaseId"] = purchase.PurchaseId;
            TempData["TotalCost"] = purchase.TotalCost.ToString("F2"); // ✅ Convert to string
            TempData["ShowConfirmationModal"] = true;

            TempData["SuccessMessage"] = "Purchase completed successfully!";
            return RedirectToAction("Confirmation", new { id = purchase.PurchaseId });
        }

        // GET: /Purchases/Confirmation/{id}
        [HttpGet("Purchases/Confirmation/{id}")]
        public async Task<IActionResult> Confirmation(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.PurchaseEvents)
                .ThenInclude(pe => pe.Event)
                .FirstOrDefaultAsync(p => p.PurchaseId == id);

            if (purchase == null)
                return NotFound();

            // ✅ PART 3.3: Pass modal data
            ViewBag.ShowConfirmationModal = TempData["ShowConfirmationModal"] as bool? ?? false;
            ViewBag.PurchaseId = TempData["PurchaseId"];
            ViewBag.TotalCost = TempData["TotalCost"] != null 
                ? decimal.Parse(TempData["TotalCost"].ToString()) 
                : 0m;
            return View(purchase);
        }

        // GET: /Purchases/History (Protected - shows only user's purchases)
        [Authorize]
        [HttpGet("Purchases/History")]
        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User);
            
            var purchases = await _context.Purchases
                .Include(p => p.PurchaseEvents)
                .ThenInclude(pe => pe.Event)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            _logger.LogInformation("User {UserId} viewed purchase history", userId); // ✅ ADD

            return View(purchases);
        }
    }
}