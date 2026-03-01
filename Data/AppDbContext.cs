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

    // Fix Decimal Precision for Ticket ONLY (since Purchase doesn't have Price)
    modelBuilder.Entity<Ticket>()
        .Property(t => t.Price)
        .HasColumnType("decimal(18,2)");

    // Mapping Relationships
    modelBuilder.Entity<Purchase>()
        .HasOne(p => p.User)
        .WithMany(u => u.Purchases)
        .HasForeignKey(p => p.UserEmail);

    modelBuilder.Entity<Ticket>()
        .HasOne(t => t.Purchase)
        .WithMany(p => p.Tickets)
        .HasForeignKey(t => t.PaymentId);
}
    }
}