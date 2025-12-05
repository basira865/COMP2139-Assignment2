namespace COMP2139_Assignment1_1.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public ICollection<Event> Events { get; set; }
    }
}
