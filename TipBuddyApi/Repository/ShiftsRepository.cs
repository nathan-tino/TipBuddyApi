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

        /// <summary>
        /// Retrieves a list of shifts, optionally filtered by a date range.
        /// </summary>
        /// <param name="startDate">Optional. Filters shifts to those with a date on or after this value.</param>
        /// <param name="endDate">Optional. Filters shifts to those with a date on or before this value.</param>
        /// <returns>A task who's result contains a list of shifts matching the specified filters.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="startDate"/> is later than <paramref name="endDate"/>.</exception>
        public async Task<List<Shift>> GetShiftsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                throw new ArgumentException("The start date cannot be after the end date.");
            }

            IQueryable<Shift> query = _context.Shifts;

            if (startDate.HasValue)
            {
                query = query.Where(s => s.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.Date < endDate.Value);
            }

            return await query.ToListAsync();
        }
    }
}
