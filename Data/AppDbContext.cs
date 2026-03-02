using Microsoft.EntityFrameworkCore;
using Concert_Backend.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

            // --- 📅 POSTGRES DATE FIX ---
            // This forces every DateTime property to be treated as UTC to prevent the Kind=Local crash
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                }
            }

            // --- 💰 Decimal Precision ---
            modelBuilder.Entity<Ticket>()
                .Property(t => t.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Concert>()
                .Property(c => c.RegularPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Concert>()
                .Property(c => c.VipPrice)
                .HasColumnType("decimal(18,2)");

            // --- 🤝 Mapping Relationships ---
            
            // User -> Purchase
            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.User)
                .WithMany(u => u.Purchases)
                .HasForeignKey(p => p.UserEmail)
                .HasPrincipalKey(u => u.Email);

            // Purchase -> Ticket
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Purchase)
                .WithMany(p => p.Tickets)
                .HasForeignKey(t => t.PaymentId);

            // Concert -> Ticket
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Concert)
                .WithMany() 
                .HasForeignKey(t => t.ConcertId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}