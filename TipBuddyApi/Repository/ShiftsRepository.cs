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
        /// <param name="userId">The ID of the user whose shifts are to be retrieved.</param>
        /// <param name="startDate">Optional. Filters shifts to those with a date on or after this value.</param>
        /// <param name="endDate">Optional. Filters shifts to those with a date on or before this value.</param>
        /// <returns>A task who's result contains a list of shifts matching the specified filters.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="startDate"/> is later than <paramref name="endDate"/>.</exception>
        public async Task<List<Shift>> GetShiftsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                throw new ArgumentException("The start date cannot be after the end date.");
            }

            IQueryable<Shift> query = _context.Shifts.Where(s => s.UserId == userId);

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

        /// <summary>
        /// Deletes all shifts for a specific user in a single database operation.
        /// This is more efficient than deleting shifts one by one as it performs
        /// a bulk delete operation directly in the database.
        /// </summary>
        /// <param name="userId">The ID of the user whose shifts should be deleted.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteShiftsByUserIdAsync(string userId)
        {
            await _context.Shifts
                .Where(s => s.UserId == userId)
                .ExecuteDeleteAsync();
        }
    }
}
