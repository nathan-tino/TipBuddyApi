using Microsoft.EntityFrameworkCore;

namespace TipBuddyApi.Data
{
    public class TipBuddyDbContext : DbContext
    {
        public DbSet<Shift> Shifts { get; set; }

        public TipBuddyDbContext(DbContextOptions options) : base(options)
        {
            
        }
    }
}
