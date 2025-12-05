using System;
using System.Collections.Generic;
using System.Linq;

namespace COMP2139_Assignment1_1.Models
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; }
        public List<Event> AvailableEvents { get; set; }
        public decimal TotalCost => CartItems.Sum(i => i.Subtotal);
    }
}