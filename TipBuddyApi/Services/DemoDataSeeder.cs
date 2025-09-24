using Microsoft.AspNetCore.Identity;
using TipBuddyApi.Contracts;
using TipBuddyApi.Data;

namespace TipBuddyApi.Services
{
    public class DemoDataSeeder : IDemoDataSeeder
    {
        private readonly UserManager<User> _userManager;
        private readonly IShiftsRepository _shiftsRepository;
        private readonly IConfiguration _configuration;

        private const string _demoUserName = "demouser";

        public DemoDataSeeder(UserManager<User> userManager, IShiftsRepository shiftsRepository, IConfiguration configuration)
        {
            _userManager = userManager;
            _shiftsRepository = shiftsRepository;
            _configuration = configuration;
        }

        public async Task SeedDemoDataAsync()
        {
            var demoUser = await _userManager.FindByNameAsync(_demoUserName);
            if (demoUser == null)
            {
                demoUser = new User { UserName = _demoUserName, Email = "demo@example.com", FirstName = "Demo", LastName = "User" };
                var password = _configuration["DemoUserPassword"] ?? "DemoPassword123!";
                var result = await _userManager.CreateAsync(demoUser, password);

                if (result.Succeeded)
                {
                    // Seed the full 30 days for a new user
                    await SeedShiftsForDateRange(demoUser.Id, DateTimeOffset.UtcNow.Date.AddDays(-29), DateTimeOffset.UtcNow.Date, new Random());
                }
            }
            else
            {
                // For an existing user, check the last 4 weeks and fill in any empty ones.
                await SeedDemoShifts(demoUser.Id);
            }
        }

        public async Task ResetDemoUserAsync()
        {
            var demoUser = await _userManager.FindByNameAsync(_demoUserName);
            if (demoUser != null)
            {
                var shifts = await _shiftsRepository.GetShiftsAsync(demoUser.Id);
                foreach (var shift in shifts)
                {
                    await _shiftsRepository.DeleteAsync(shift.Id);
                }
                await _userManager.DeleteAsync(demoUser);
            }

            await SeedDemoDataAsync();
        }

        private async Task SeedDemoShifts(string userId)
        {
            var today = DateTimeOffset.UtcNow.Date;
            var random = new Random();

            // Check the last 4 weeks (as 4 7-day chunks)
            for (int w = 0; w < 4; w++)
            {
                var endDate = today.AddDays(-(w * 7));
                var startDate = endDate.AddDays(-6);

                var existingShifts = await _shiftsRepository.GetShiftsAsync(userId, startDate, endDate);
                if (!existingShifts.Any())
                {
                    await SeedShiftsForDateRange(userId, startDate, endDate, random);
                }
            }
        }

        private async Task SeedShiftsForDateRange(string userId, DateTimeOffset startDate, DateTimeOffset endDate, Random random)
        {
            var shiftsToAdd = new List<Shift>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                int numberOfShifts = random.Next(0, 3); // 0, 1, or 2 shifts

                if (numberOfShifts == 1)
                {
                    shiftsToAdd.Add(CreateShift(userId, date, random, random.Next(4, 9)));
                }
                else if (numberOfShifts == 2)
                {
                    int totalHours = random.Next(8, 13); // Total hours for the day (8-12)
                    int hours1 = totalHours / 2;
                    int hours2 = totalHours - hours1;

                    shiftsToAdd.Add(CreateShift(userId, date, random, hours1));
                    shiftsToAdd.Add(CreateShift(userId, date, random, hours2));
                }
                // If numberOfShifts is 0, no shift is created for this day.
            }

            foreach (var shift in shiftsToAdd)
            {
                await _shiftsRepository.AddAsync(shift);
            }
        }

        private Shift CreateShift(string userId, DateTimeOffset date, Random random, int hours)
        {
            return new Shift
            {
                UserId = userId,
                Date = date,
                CreditTips = random.Next(50, 201),
                CashTips = random.Next(0, 101),
                Tipout = random.Next(1, 11),
                HoursWorked = hours
            };
        }
    }
}
