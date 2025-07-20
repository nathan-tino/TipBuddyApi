using Microsoft.EntityFrameworkCore;

namespace TipBuddyApi.Data
{
    public class TipBuddyDbContext : DbContext
    {
        public DbSet<Shift> Shifts { get; set; }

        public TipBuddyDbContext(DbContextOptions options) : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Shift>()
                .Property(s => s.Date)
                .HasColumnType("datetime2(0)");
        }
    }
}
