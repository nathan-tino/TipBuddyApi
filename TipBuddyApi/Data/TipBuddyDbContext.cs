using Microsoft.EntityFrameworkCore;

namespace TipBuddyApi.Data
{
    public class TipBuddyDbContext : DbContext
    {
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<User> Users { get; set; }

        public TipBuddyDbContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Shift entity configuration
            modelBuilder.Entity<Shift>()
                .Property(s => s.Date)
                .HasColumnType("datetimeoffset(0)");

            modelBuilder.Entity<Shift>()
                .Property(s => s.CreatedAt)
                .HasColumnType("datetimeoffset(0)");

            modelBuilder.Entity<Shift>()
                .Property(s => s.UpdatedAt)
                .HasColumnType("datetimeoffset(0)");

            modelBuilder.Entity<Shift>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User entity configuration
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasColumnType("datetimeoffset(0)");

            modelBuilder.Entity<User>()
                .Property(u => u.UpdatedAt)
                .HasColumnType("datetimeoffset(0)");
        }
    }
}
