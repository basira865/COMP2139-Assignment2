using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace COMP2139_Assignment1_1.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FullName { get; set; }
        
        [StringLength(200)]
        public string? ProfilePicturePath { get; set; }
        
        // Navigation properties
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    }
}