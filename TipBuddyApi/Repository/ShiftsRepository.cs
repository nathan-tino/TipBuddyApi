using Microsoft.EntityFrameworkCore;
using TipBuddyApi.Contracts;
using TipBuddyApi.Data;

namespace TipBuddyApi.Repository
{
    public class ShiftsRepository : GenericRepository<Shift>, IShiftsRepository
    {
        private TipBuddyDbContext _context;

        public ShiftsRepository(TipBuddyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Shift>> GetShiftsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            IQueryable<Shift> query = _context.Shifts;

            if (startDate.HasValue)
            {
                query = query.Where(s => s.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.Date <= endDate.Value);
            }

            return await query.ToListAsync();
        }
    }
}
