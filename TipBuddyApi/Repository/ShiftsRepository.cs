using TipBuddyApi.Contracts;
using TipBuddyApi.Data;

namespace TipBuddyApi.Repository
{
    public class ShiftsRepository : GenericRepository<Shift>, IShiftsRepository
    {
        public ShiftsRepository(TipBuddyDbContext context) : base(context)
        {
            
        }
    }
}
