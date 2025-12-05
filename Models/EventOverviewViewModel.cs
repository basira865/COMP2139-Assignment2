using System.Collections.Generic;

namespace COMP2139_Assignment1_1.Models
{
    public class EventOverviewViewModel
    {
        public int TotalEvents { get; set; }
        public int TotalCategories { get; set; }
        public List<Event> LowTicketEvents { get; set; } = new();
    }
}
