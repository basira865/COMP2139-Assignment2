namespace COMP2139_Assignment1_1.Models
{
   public class CartItem
{
    public int EventId { get; set; }
    public string EventTitle { get; set; }
    public string CategoryName { get; set; }
    public DateTime EventDateTime { get; set; }
    public decimal TicketPrice { get; set; }
    public int AvailableTickets { get; set; }
    public int Quantity { get; set; }

    public decimal Subtotal => TicketPrice * Quantity;
}
}