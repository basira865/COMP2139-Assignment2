using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using COMP2139_Assignment1_1.Models;

namespace COMP2139_Assignment1_1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseEvent> PurchaseEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // PurchaseEvent composite key
            modelBuilder.Entity<PurchaseEvent>()
                .HasKey(pe => new { pe.PurchaseId, pe.EventId });

            // Purchase -> User relationship
            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.User)
                .WithMany(u => u.Purchases)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Event -> Organizer relationship
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Music", Description = "Live and recorded music events" },
                new Category { CategoryId = 2, Name = "Tech", Description = "Technology conferences and expos" },
                new Category { CategoryId = 3, Name = "Art", Description = "Art exhibitions and creative showcases" }
            );

            // Seed Events
            modelBuilder.Entity<Event>().HasData(
                new Event
                {
                    EventId = 1,
                    Title = "Jazz Night",
                    DateTime = new DateTime(2025, 10, 17, 19, 0, 0),
                    ImageUrl = "/images/jazz.webp",
                    TicketPrice = 25.00m,
                    AvailableTickets = 100,
                    CategoryId = 1
                },
                new Event
                {
                    EventId = 2,
                    Title = "AI Expo",
                    DateTime = new DateTime(2025, 12, 5, 10, 0, 0),
                    ImageUrl = "/images/techExpo.webp",
                    TicketPrice = 50.00m,
                    AvailableTickets = 200,
                    CategoryId = 2
                },
                new Event
                {
                    EventId = 3,
                    Title = "Gallery Showcase",
                    DateTime = new DateTime(2025, 11, 20, 17, 30, 0),
                    ImageUrl = "/images/gallery.webp",
                    TicketPrice = 15.00m,
                    AvailableTickets = 80,
                    CategoryId = 3
                }
            );
        }
    }
}