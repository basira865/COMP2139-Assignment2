using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using COMP2139_Assignment1_1.Data;
using COMP2139_Assignment1_1.Models;
using System.Diagnostics; // ✅ ADD THIS
using Microsoft.Extensions.Logging; // ✅ ADD THIS

namespace COMP2139_Assignment1_1.Controllers  
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger; // ✅ ADD THIS

        // ✅ UPDATE CONSTRUCTOR
        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger; // ✅ ADD THIS
        }

        // ✅ KEEP YOUR EXISTING INDEX METHOD
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.Include(e => e.Category).ToListAsync();
            var categories = await _context.Categories.ToListAsync();
            
            var viewModel = new EventListViewModel
            {
                EventList = events,
                CategoryList = new SelectList(categories, "CategoryId", "Name"),
                CategoryFilter = null
            };
            
            return View(viewModel);
        }

        // ✅ ADD THESE NEW METHODS BELOW (Don't remove anything above)
        
        // Handle 500 errors
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Error page displayed. Request ID: {RequestId}", requestId);
            
            return View(new ErrorViewModel 
            { 
                RequestId = requestId,
                Message = "An unexpected error occurred. Our team has been notified."
            });
        }

        // Handle 404 and other status codes
        [Route("/Home/StatusCode")]
        public IActionResult StatusCode(int code)
        {
            _logger.LogWarning("Status code {StatusCode} returned for path: {Path}", 
                code, HttpContext.Request.Path);

            switch (code)
            {
                case 404:
                    return View("NotFound");
                case 403:
                    return View("Forbidden");
                default:
                    return View("Error", new ErrorViewModel 
                    { 
                        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                        Message = $"Status code: {code}"
                    });
            }
        }
    }
}