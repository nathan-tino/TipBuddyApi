using TipBuddyApi.Data;

namespace TipBuddyApi.Contracts
{
    public interface IShiftsRepository : IGenericRepository<Shift>
    {
        Task<List<Shift>> GetShiftsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
