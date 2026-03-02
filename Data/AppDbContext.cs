using Microsoft.EntityFrameworkCore;
using Concert_Backend.Models;

namespace Concert_Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Concert> Concerts { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // 💰 Fix Decimal Precision for Tickets table (used for individual sales records)
    modelBuilder.Entity<Ticket>()
        .Property(t => t.Price)
        .HasColumnType("decimal(18,2)");

    // 🎤 Updated Concert Table Precision (handling both price tiers)
    modelBuilder.Entity<Concert>()
        .Property(c => c.RegularPrice)
        .HasColumnType("decimal(18,2)");

    modelBuilder.Entity<Concert>()
        .Property(c => c.VipPrice)
        .HasColumnType("decimal(18,2)");

    // 🤝 Mapping Relationships
    
    // User -> Purchase (One-to-Many)
    modelBuilder.Entity<Purchase>()
        .HasOne(p => p.User)
        .WithMany(u => u.Purchases)
        .HasForeignKey(p => p.UserEmail)
        .HasPrincipalKey(u => u.Email); // Explicitly link to Email if it's the FK

    // Purchase -> Ticket (One-to-Many)
    modelBuilder.Entity<Ticket>()
        .HasOne(t => t.Purchase)
        .WithMany(p => p.Tickets)
        .HasForeignKey(t => t.PaymentId);

    // Concert -> Ticket (One-to-Many)
    modelBuilder.Entity<Ticket>()
        .HasOne(t => t.Concert)
        .WithMany() 
        .HasForeignKey(t => t.ConcertId)
        .OnDelete(DeleteBehavior.Cascade); // 👈 Important: Deleting a concert cleans up its tickets
}
    }
}