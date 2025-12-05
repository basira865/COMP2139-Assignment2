using System.Text.Json;
using System.Linq;
using Microsoft.AspNetCore.Http;
using COMP2139_Assignment1_1.Models;

namespace COMP2139_Assignment1_1.Helpers
{
    public static class CartHelper
    {
        private const string CartSessionKey = "ShoppingCart";

        public static List<CartItem> GetCart(ISession session)
        {
            var cartJson = session.GetString(CartSessionKey);
            return string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        public static void SaveCart(ISession session, List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            session.SetString(CartSessionKey, cartJson);
        }

        public static void AddToCart(ISession session, Event ev, int quantity = 1)
        {
            var cart = GetCart(session);

            var existingItem = cart.FirstOrDefault(c => c.EventId == ev.EventId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    EventId = ev.EventId,
                    EventTitle = ev.Title,
                    CategoryName = ev.Category?.Name ?? "Uncategorized",
                    EventDateTime = ev.DateTime.UtcDateTime,
                    TicketPrice = ev.TicketPrice,
                    AvailableTickets = ev.AvailableTickets,
                    Quantity = quantity
                });
            }

            SaveCart(session, cart);
        }

        // ✅ NEW METHOD - Update quantity for existing cart item
        public static void UpdateQuantity(ISession session, int eventId, int newQuantity)
        {
            var cart = GetCart(session);
            var item = cart.FirstOrDefault(c => c.EventId == eventId);
            
            if (item != null)
            {
                if (newQuantity <= 0)
                {
                    // Remove item if quantity is 0 or negative
                    cart.RemoveAll(c => c.EventId == eventId);
                }
                else
                {
                    // Update quantity
                    item.Quantity = newQuantity;
                }
                
                SaveCart(session, cart);
            }
        }

        public static void RemoveFromCart(ISession session, int eventId)
        {
            var cart = GetCart(session);
            cart.RemoveAll(c => c.EventId == eventId);
            SaveCart(session, cart);
        }

        public static void ClearCart(ISession session)
        {
            session.Remove(CartSessionKey);
        }

        public static int GetCartCount(ISession session)
        {
            var cart = GetCart(session);
            return cart.Sum(c => c.Quantity);
        }

        public static decimal GetCartTotal(ISession session)
        {
            var cart = GetCart(session);
            return cart.Sum(c => c.Subtotal);
        }
    }
}
