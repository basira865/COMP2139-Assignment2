
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using COMP2139_Assignment1_1.Data;
using COMP2139_Assignment1_1.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace COMP2139_Assignment1_1.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EventsController> _logger; // ✅ ADD THIS LINE

        public EventsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<EventsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger; // ✅ ADD THIS LINE
        }

        // GET: Events (Public - no auth required)
        public async Task<IActionResult> Index(string search, int? categoryFilter, DateTimeOffset? startDate,
            DateTimeOffset? endDate, string sortOrder)
        {
            var categories = await _context.Categories.ToListAsync();
            var eventsQuery = _context.Events.Include(e => e.Category).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                eventsQuery = eventsQuery.Where(e => e.Title != null && e.Title.ToLower().Contains(search.ToLower()));
            }

            if (categoryFilter.HasValue)
            {
                eventsQuery = eventsQuery.Where(e => e.CategoryId == categoryFilter.Value);
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                eventsQuery = eventsQuery.Where(e => e.DateTime >= startDate.Value && e.DateTime <= endDate.Value);
            }

            eventsQuery = sortOrder switch
            {
                "title" => eventsQuery.OrderBy(e => e.Title),
                "date" => eventsQuery.OrderBy(e => e.DateTime),
                "price" => eventsQuery.OrderBy(e => e.TicketPrice),
                _ => eventsQuery.OrderBy(e => e.EventId)
            };

            var viewModel = new EventListViewModel
            {
                EventList = await eventsQuery.ToListAsync(),
                CategoryList = new SelectList(categories, "CategoryId", "Name"),
                CategoryFilter = categoryFilter
            };

            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Search(string search, int? categoryFilter)
        {
            try
            {
                var eventsQuery = _context.Events.Include(e => e.Category).AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    eventsQuery = eventsQuery.Where(e => e.Title != null && e.Title.ToLower().Contains(search.ToLower()));
                }

                if (categoryFilter.HasValue && categoryFilter.Value > 0)
                {
                    eventsQuery = eventsQuery.Where(e => e.CategoryId == categoryFilter.Value);
                }

                var events = await eventsQuery.ToListAsync();

                return PartialView("_EventPartial", events);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Search failed");
                return Content("<div class='alert alert-danger'>Error loading events</div>", "text/html");
            }
        }
        [HttpGet]
        public IActionResult TestPartial()
        {
            try
            {
                var events = _context.Events.Include(e => e.Category).Take(5).ToList();
                return PartialView("_EventPartial", events);
            }
            catch (Exception ex)
            {
                return Content($"ERROR: {ex.Message}<br>STACK: {ex.StackTrace}", "text/html");
            }
        }
        // GET: Events/Create (Protected)
        [Authorize(Roles = "Admin,Organizer")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        // POST: Events/Create (Protected)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> Create(
            [Bind("EventId,Title,DateTime,TicketPrice,AvailableTickets,CategoryId,ImageUrl")]
            Event @event)
        {
            if (ModelState.IsValid)
            {
                @event.DateTime = @event.DateTime.ToUniversalTime();
                @event.OrganizerId = _userManager.GetUserId(User);

                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", @event.CategoryId);
            return View(@event);
        }

        // GET: Events/Details/5 (Public)
        public async Task<IActionResult> Details(int id)
        {
            var @event = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(m => m.EventId == id);

            if (@event == null) return NotFound();

            return View(@event);
        }

        // GET: Events/Edit/5 (Protected - only own events)
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> Edit(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();

            // Check if user owns this event (unless admin)
            if (!User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                if (@event.OrganizerId != userId)
                    return Forbid();
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", @event.CategoryId);
            return View(@event);
        }

        // POST: Events/Edit/5 (Protected)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> Edit(int id,
            [Bind("EventId,Title,DateTime,TicketPrice,AvailableTickets,CategoryId,ImageUrl")]
            Event @event)
        {
            if (id != @event.EventId) return NotFound();

            // Check ownership
            var existingEvent = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id);
            if (!User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                if (existingEvent.OrganizerId != userId)
                    return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    @event.DateTime = @event.DateTime.ToUniversalTime();
                    @event.OrganizerId = existingEvent.OrganizerId; // Preserve original organizer
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Events.Any(e => e.EventId == @event.EventId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", @event.CategoryId);
            return View(@event);
        }

        // GET: Events/Delete/5 (Protected)
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> Delete(int id)
        {
            var @event = await _context.Events
                .Include(e => e.Category)
                .FirstOrDefaultAsync(m => m.EventId == id);

            if (@event == null) return NotFound();

            // Check ownership
            if (!User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                if (@event.OrganizerId != userId)
                    return Forbid();
            }

            return View(@event);
        }

        // POST: Events/Delete/5 (Protected)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                // Check ownership
                if (!User.IsInRole("Admin"))
                {
                    var userId = _userManager.GetUserId(User);
                    if (@event.OrganizerId != userId)
                        return Forbid();
                }

                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Events/Overview (Protected - Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Overview()
        {
            var totalEvents = await _context.Events.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();
            var lowTicketEvents = await _context.Events
                .Include(e => e.Category)
                .Where(e => e.AvailableTickets <= 5)
                .ToListAsync();

            var viewModel = new EventOverviewViewModel
            {
                TotalEvents = totalEvents,
                TotalCategories = totalCategories,
                LowTicketEvents = lowTicketEvents
            };

            return View(viewModel);
        }
        // ==================== ANALYTICS DASHBOARD ====================

        // GET: Events/MyAnalytics - Analytics Dashboard View
        [HttpGet]
        [Authorize(Roles = "Organizer,Admin")]
        public IActionResult MyAnalytics()
        {
            return View();
        }

        // GET: Events/GetAnalyticsData - JSON API for Charts
        [HttpGet]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> GetAnalyticsData()
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            // Base query for purchases
            var purchasesQuery = _context.PurchaseEvents
                .Include(pe => pe.Event)
                .ThenInclude(e => e.Category)
                .AsQueryable();

            //  FILTERING: Organizers see only THEIR events, Admins see ALL
            if (!isAdmin)
            {
                purchasesQuery = purchasesQuery.Where(pe => pe.Event.OrganizerId == currentUser.Id);
            }

            // --- CHART 1: Sales by Category ---
            var salesByCategory = await purchasesQuery
                .GroupBy(pe => pe.Event.Category.Name)
                .Select(g => new
                {
                    category = g.Key,
                    ticketsSold = g.Sum(pe => pe.Quantity)
                })
                .ToListAsync();

            // --- CHART 2: Revenue by Month ---
            var revenueData = await _context.Purchases
                .Include(p => p.PurchaseEvents)
                .ThenInclude(pe => pe.Event)
                .ToListAsync();

            // Filter purchases if Organizer
            if (!isAdmin)
            {
                revenueData = revenueData
                    .Where(p => p.PurchaseEvents.Any(pe => pe.Event.OrganizerId == currentUser.Id))
                    .ToList();
            }

            var revenueByMonth = revenueData
                .GroupBy(p => new
                {
                    Year = p.PurchaseDate.Year,
                    Month = p.PurchaseDate.Month
                })
                .Select(g => new
                {
                    month = g.Key.Month + "/" + g.Key.Year,
                    revenue = g.Sum(p => p.TotalCost)
                })
                .OrderBy(x => x.month)
                .ToList();

            // --- TABLE: Top 5 Best-Selling Events ---
            var topEvents = await purchasesQuery
                .GroupBy(pe => new
                {
                    pe.Event.EventId,
                    pe.Event.Title
                })
                .Select(g => new
                {
                    title = g.Key.Title,
                    ticketsSold = g.Sum(pe => pe.Quantity),
                    revenue = g.Sum(pe => pe.TotalPrice)
                })
                .OrderByDescending(x => x.ticketsSold)
                .Take(5)
                .ToListAsync();

            // --- Return Combined JSON ---
            var analyticsData = new
            {
                salesByCategory = salesByCategory,
                revenueByMonth = revenueByMonth,
                topEvents = topEvents
            };

            return Json(analyticsData);
        }
        // GET: Events/SeedAnalyticsData - Helper to create sample data for testing
            [HttpGet]
            [Authorize(Roles = "Admin")] // ⚠️ ADMIN ONLY
            public async Task<IActionResult> SeedAnalyticsData()
            {
                //  Safety check: Only allow in development or if no data exists
                var existingPurchases = await _context.Purchases.CountAsync();
                if (existingPurchases > 50) // Prevent accidental re-seeding
                {
                    return Json(new { error = "Data already exists. Delete purchases first if you want to re-seed." });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                var events = await _context.Events.Take(3).ToListAsync();
                
                if (!events.Any())
                    return Json(new { error = "No events found. Create events first." });

                var testPurchases = new List<Purchase>();
                
                // Create purchases for the last 6 months
                for (int monthsAgo = 5; monthsAgo >= 0; monthsAgo--)
                {
                    var purchaseDate = DateTime.UtcNow.AddMonths(-monthsAgo);
                    var randomEvent = events[monthsAgo % events.Count];
                    
                    var purchase = new Purchase
                    {
                        UserId = currentUser.Id,
                        GuestName = currentUser.FullName ?? "Test User",
                        GuestEmail = currentUser.Email ?? "test@test.com",
                        PurchaseDate = purchaseDate,
                        TotalCost = 100.00m * (monthsAgo + 1),
                        PurchaseEvents = new List<PurchaseEvent>
                        {
                            new PurchaseEvent
                            {
                                EventId = randomEvent.EventId,
                                Quantity = (monthsAgo + 1) * 2,
                                TotalPrice = 100.00m * (monthsAgo + 1)
                            }
                        }
                    };
                    
                    testPurchases.Add(purchase);
                }
                
                _context.Purchases.AddRange(testPurchases);
                await _context.SaveChangesAsync();
                
                return Json(new
                {
                    message = " Sample analytics data created successfully!",
                    purchasesCreated = testPurchases.Count,
                    note = "This is for demonstration purposes only"
                });
            }
    }
}