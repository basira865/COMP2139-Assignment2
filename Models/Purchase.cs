using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace COMP2139_Assignment1_1.Models
{
    public class Purchase
    {
        public int PurchaseId { get; set; }

        [Required]
        [StringLength(100)]
        public string GuestName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string GuestEmail { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        public DateTime PurchaseDate { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalCost { get; set; }

        // NEW: Link to authenticated user (nullable for guest purchases)
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // NEW: Rating for past events
        [Range(1, 5)]
        public int? Rating { get; set; }

        public ICollection<PurchaseEvent> PurchaseEvents { get; set; } = new List<PurchaseEvent>();
    }
}