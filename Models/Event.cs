using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace COMP2139_Assignment1_1.Models
{
    public class Event
    {
        public int EventId { get; set; }

        [Required]
        public string Title { get; set; }

        public string? ImageUrl { get; set; }   

        [Required]
        public DateTimeOffset DateTime { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Ticket price must be non-negative")]
        public decimal TicketPrice { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Available tickets must be non-negative")]
        public int AvailableTickets { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        public int? CategoryId { get; set; }

        [ValidateNever]
        public Category Category { get; set; }

        // NEW: Link to organizer
        public string? OrganizerId { get; set; }
        public ApplicationUser? Organizer { get; set; }

        public bool IsSoldOut => AvailableTickets <= 0;

        public ICollection<PurchaseEvent> PurchaseEvents { get; set; } = new List<PurchaseEvent>();
    }
}