using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COMP2139_Assignment1_1.Data;
using COMP2139_Assignment1_1.Models;
using COMP2139_Assignment1_1.Helpers;

namespace COMP2139_Assignment1_1.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger; // ✅ ADD

        public CartController(ApplicationDbContext context, ILogger<CartController> logger) // ✅ ADD logger
        {
            _context = context;
            _logger = logger; // ✅ ADD
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cartItems = CartHelper.GetCart(HttpContext.Session);
            ViewBag.TotalCost = cartItems.Sum(i => i.Subtotal);
            return View(cartItems);
        }

        // GET: /Cart/Add/5
        public async Task<IActionResult> Add(int id)
        {
            var @event = await _context.Events
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (@event == null)
                return NotFound();

            var cartItem = new CartItem
            {
                EventId = @event.EventId,
                EventTitle = @event.Title,
                EventDateTime = @event.DateTime.UtcDateTime,
                CategoryName = @event.Category?.Name ?? "N/A",
                TicketPrice = @event.TicketPrice,
                AvailableTickets = @event.AvailableTickets,
                Quantity = 1
            };

            return View(cartItem);
        }

        // POST: /Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id, int quantity)
        {
            var @event = await _context.Events
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (@event == null)
                return NotFound();

            if (@event.AvailableTickets < quantity)
            {
                ModelState.AddModelError(string.Empty, $"Only {@event.AvailableTickets} tickets available.");
                return View(new CartItem
                {
                    EventId = @event.EventId,
                    EventTitle = @event.Title,
                    EventDateTime = @event.DateTime.UtcDateTime,
                    CategoryName = @event.Category?.Name ?? "N/A",
                    TicketPrice = @event.TicketPrice,
                    AvailableTickets = @event.AvailableTickets,
                    Quantity = quantity
                });
            }

            if (!ModelState.IsValid)
                return View();

            CartHelper.AddToCart(HttpContext.Session, @event, quantity);

            TempData["SuccessMessage"] = $"Added {quantity} ticket(s) for '{@event.Title}' to cart!";
            return RedirectToAction(nameof(Index));
        }

        // ✅ PART 3.2: AJAX Add to Cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int eventId, int quantity)
        {
            try
            {
                var evt = await _context.Events.FindAsync(eventId);
                if (evt == null)
                {
                    _logger.LogWarning("Add to cart failed: Event {EventId} not found", eventId);
                    return Json(new { success = false, message = "Event not found" });
                }

                if (evt.AvailableTickets < quantity)
                {
                    _logger.LogWarning("Add to cart failed: Not enough tickets for Event {EventId}", eventId);
                    return Json(new { success = false, message = $"Only {evt.AvailableTickets} tickets available" });
                }

                CartHelper.AddToCart(HttpContext.Session, evt, quantity);
                
                var cartItems = CartHelper.GetCart(HttpContext.Session);
                var cartCount = cartItems.Sum(i => i.Quantity);
                var cartTotal = cartItems.Sum(i => i.Subtotal);

                _logger.LogInformation("Added {Quantity} tickets for Event {EventId} to cart", quantity, eventId);

                return Json(new
                {
                    success = true,
                    cartCount = cartCount,
                    cartTotal = cartTotal,
                    availableTickets = evt.AvailableTickets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // ✅ PART 3.2: AJAX Update Quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int eventId, int quantity)
        {
            try
            {
                var evt = await _context.Events.FindAsync(eventId);
                if (evt == null)
                    return Json(new { success = false, message = "Event not found" });

                if (quantity > evt.AvailableTickets)
                    return Json(new { success = false, message = $"Only {evt.AvailableTickets} tickets available" });

                CartHelper.UpdateQuantity(HttpContext.Session, eventId, quantity);
                
                var cartItems = CartHelper.GetCart(HttpContext.Session);
                var cartItem = cartItems.FirstOrDefault(i => i.EventId == eventId);
                var cartCount = cartItems.Sum(i => i.Quantity);
                var cartTotal = cartItems.Sum(i => i.Subtotal);

                return Json(new
                {
                    success = true,
                    cartCount = cartCount,
                    cartTotal = cartTotal,
                    itemSubtotal = cartItem?.Subtotal ?? 0,
                    availableTickets = evt.AvailableTickets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int eventId)
        {
            CartHelper.RemoveFromCart(HttpContext.Session, eventId);
            return RedirectToAction(nameof(Index));
        }

        // ✅ PART 3.2: AJAX Remove from Cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int eventId)
        {
            try
            {
                CartHelper.RemoveFromCart(HttpContext.Session, eventId);
                
                var cartItems = CartHelper.GetCart(HttpContext.Session);
                var cartCount = cartItems.Sum(i => i.Quantity);
                var cartTotal = cartItems.Sum(i => i.Subtotal);

                _logger.LogInformation("Removed Event {EventId} from cart", eventId);

                return Json(new
                {
                    success = true,
                    cartCount = cartCount,
                    cartTotal = cartTotal
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        // POST: /Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            CartHelper.ClearCart(HttpContext.Session);
            return RedirectToAction(nameof(Index));
        }
    }
}