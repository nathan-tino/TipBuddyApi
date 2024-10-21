using TipBuddyApi.Data;

namespace TipBuddyApi.Contracts
{
    public interface IShiftsRepository : IGenericRepository<Shift>
    {
        Task<List<Shift>> GetShiftsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}
