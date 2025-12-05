using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace COMP2139_Assignment1_1.Models
{
    public class EventListViewModel
    {
        public int? CategoryFilter { get; set; }
        public IEnumerable<SelectListItem> CategoryList { get; set; }
        public List<Event> EventList { get; set; }
    }
}
