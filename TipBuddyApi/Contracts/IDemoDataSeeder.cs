using System.Threading.Tasks;

namespace TipBuddyApi.Contracts
{
    public interface IDemoDataSeeder
    {
        Task SeedDemoDataAsync();
        Task ResetDemoUserAsync();
        Task ResetDemoUserShiftsAsync();
    }
}
