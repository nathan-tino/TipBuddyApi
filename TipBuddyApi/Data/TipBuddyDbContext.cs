using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TipBuddyApi.Data
{
    // This class uses "PrimaryConstructor" feature of C# 9.0 to simplify the DbContext constructor
    public class TipBuddyDbContext(DbContextOptions<TipBuddyDbContext> options) : IdentityDbContext<User>(options)
    {
        public DbSet<Shift> Shifts { get; set; }

        //TODO: Switch to use IdentityUser for authentication and authorization
        public DbSet<User> Users { get; set; }

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
