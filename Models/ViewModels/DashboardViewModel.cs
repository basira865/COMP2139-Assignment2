using System.Collections.Generic;
using System.Linq;
using COMP2139_Assignment1_1.Models; // for Purchase, Event, ApplicationUser

namespace COMP2139_Assignment1_1.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Section 1: My Tickets (Upcoming Events)
        public List<Purchase> MyTickets { get; set; } = new();

        // Section 2: Purchase History (Past Events)
        public List<Purchase> PurchaseHistory { get; set; } = new();

        // Section 3: My Events (Organizers only)
        public List<Event> MyEvents { get; set; } = new();

        // Section 4: User Profile
        public ApplicationUser UserProfile { get; set; }

        // Calculated properties
        public decimal TotalRevenue
        {
            get
            {
                if (MyEvents == null || !MyEvents.Any())
                    return 0;

                return MyEvents
                    .SelectMany(e => e.PurchaseEvents)
                    .Sum(pe => pe.TotalPrice);
            }
        }

        public int TotalTicketsSold
        {
            get
            {
                if (MyEvents == null || !MyEvents.Any())
                    return 0;

                return MyEvents
                    .SelectMany(e => e.PurchaseEvents)
                    .Sum(pe => pe.Quantity);
            }
        }
    }
}