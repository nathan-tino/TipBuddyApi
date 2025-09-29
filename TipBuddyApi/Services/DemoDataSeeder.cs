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
        private readonly ILogger<DemoDataSeeder> _logger;
        private readonly ITimeZoneService _timeZoneService;

        private const string DemoUserName = "demouser";
        private const int MaxHistoryDays = 60; // 2 months

        // Shift probability constants
        private const double MondayTuesdayShiftProbability = 0.10;
        private const double WednesdayThursdayShiftProbability = 0.75;
        private const double FridaySundayShiftProbability = 0.90;
        private const double DoubleShiftProbability = 0.25;

        // Shift timing constants
        private const int MorningShiftStartHour = 8;
        private const int MorningShiftEndHour = 12;
        private const int AfternoonShiftStartHour = 12;
        private const int AfternoonShiftEndHour = 16;
        private const int EveningShiftStartHour = 16;
        private const int EveningShiftEndHour = 20;

        // Shift duration constants
        private const int MinimumShiftDurationHours = 3;
        private const int MaximumSingleShiftDurationHours = 8;
        private const int MaximumFirstDoubleShiftDurationHours = 6;
        private const int MaximumDailyHours = 12;
        private const int MaximumDayHour = 23; // 11 PM

        // Break time constants
        private const int MinimumBreakBetweenShiftsHours = 1;
        private const int MaximumBreakBetweenShiftsHours = 3;

        // Shift timing granularity
        private const int MinuteIntervals = 15;
        private const int MinuteIntervalsPerHour = 4; // 60 minutes / 15 minute intervals

        // Tip generation constants
        private const int MinimumCreditTips = 50;
        private const int MaximumCreditTips = 201; // Random.Next is exclusive of upper bound
        private const int MinimumCashTips = 0;
        private const int MaximumCashTips = 101;
        private const int MinimumTipout = 1;
        private const int MaximumTipout = 11;

        public DemoDataSeeder(
            UserManager<User> userManager, 
            IShiftsRepository shiftsRepository, 
            IConfiguration configuration, 
            ILogger<DemoDataSeeder> logger,
            ITimeZoneService timeZoneService)
        {
            _userManager = userManager;
            _shiftsRepository = shiftsRepository;
            _configuration = configuration;
            _logger = logger;
            _timeZoneService = timeZoneService;
        }

        public async Task SeedDemoDataAsync()
        {
            _logger.LogInformation("Starting demo data seeding for user '{DemoUserName}'", DemoUserName);
            
            var demoUser = await _userManager.FindByNameAsync(DemoUserName);
            if (demoUser == null)
            {
                _logger.LogInformation("Demo user not found, creating new demo user");
                demoUser = new User { UserName = DemoUserName, Email = "demo@example.com", FirstName = "Demo", LastName = "User" };
                var password = _configuration["DemoUserPassword"] ?? "DemoPassword123!";
                var result = await _userManager.CreateAsync(demoUser, password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Demo user created successfully, seeding {Days} days of shift data", MaxHistoryDays);
                    await SeedFullHistoryForUser(demoUser.Id);
                }
                else
                {
                    _logger.LogError("Failed to create demo user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation("Demo user found, checking for data gaps");
                await FillInSinceLastShift(demoUser.Id);
            }
        }

        public async Task ResetDemoUserAsync()
        {
            _logger.LogWarning("Resetting demo user - deleting all data and recreating");
            
            var demoUser = await _userManager.FindByNameAsync(DemoUserName);
            if (demoUser != null)
            {
                await _shiftsRepository.DeleteShiftsByUserIdAsync(demoUser.Id);
                await _userManager.DeleteAsync(demoUser);
            }

            await SeedDemoDataAsync();
        }

        public async Task ResetDemoUserShiftsAsync()
        {
            _logger.LogWarning("Resetting demo user shifts - deleting all shift data");
            
            var demoUser = await _userManager.FindByNameAsync(DemoUserName);
            if (demoUser != null)
            {
                await _shiftsRepository.DeleteShiftsByUserIdAsync(demoUser.Id);
                await SeedFullHistoryForUser(demoUser.Id);
            }
        }

        private async Task FillInSinceLastShift(string userId)
        {
            var today = _timeZoneService.GetCurrentLocalDate();
            
            // Get ALL shifts for this user to find the most recent one
            var allShifts = await _shiftsRepository.GetShiftsAsync(userId);
            
            if (allShifts.Count == 0)
            {
                _logger.LogInformation("No existing shifts found, seeding full {Days} day history", MaxHistoryDays);
                await SeedFullHistoryForUser(userId);
                return;
            }

            // Find the most recent shift date (convert to configured timezone for comparison)
            var mostRecentShiftDate = allShifts.Max(s => _timeZoneService.GetLocalDate(s.Date)).Date;
            var daysSinceLastShift = (today - mostRecentShiftDate).Days;

            _logger.LogInformation("Found {ShiftCount} existing shifts, most recent: {MostRecentDate}, days since: {DaysSince}", 
                allShifts.Count, mostRecentShiftDate.ToString("yyyy-MM-dd"), daysSinceLastShift);

            if (daysSinceLastShift > MaxHistoryDays)
            {
                _logger.LogWarning("Gap of {Days} days exceeds maximum history ({MaxDays} days), regenerating fresh data", 
                    daysSinceLastShift, MaxHistoryDays);
                await _shiftsRepository.DeleteShiftsByUserIdAsync(userId);
                await SeedFullHistoryForUser(userId);
                return;
            }

            if (daysSinceLastShift <= 0)
            {
                _logger.LogInformation("No gap detected, demo data is current");
                return;
            }

            _logger.LogInformation("Filling gap of {Days} days since last shift", daysSinceLastShift);
            
            // Generate shifts for each day since the last shift (exclusive) up to today (inclusive)
            var datesToSeed = new List<DateTime>();
            for (var date = mostRecentShiftDate.AddDays(1); date <= today; date = date.AddDays(1))
            {
                datesToSeed.Add(date);
            }

            if (datesToSeed.Any())
            {
                await SeedShiftsForDates(userId, datesToSeed);
            }
        }

        private async Task SeedFullHistoryForUser(string userId)
        {
            var today = _timeZoneService.GetCurrentLocalDate();
            var startDate = today.AddDays(-MaxHistoryDays + 1); // 60 days including today
            
            _logger.LogInformation("Seeding full history from {StartDate} to {EndDate} ({Days} days)", 
                startDate.ToString("yyyy-MM-dd"), today.ToString("yyyy-MM-dd"), MaxHistoryDays);
            
            var datesToSeed = new List<DateTime>();
            for (var date = startDate; date <= today; date = date.AddDays(1))
            {
                datesToSeed.Add(date);
            }

            await SeedShiftsForDates(userId, datesToSeed);
        }

        private async Task SeedShiftsForDates(string userId, List<DateTime> dates)
        {
            if (dates.Count == 0)
            {
                _logger.LogInformation("No dates to seed");
                return;
            }

            _logger.LogInformation("Generating shifts for {DateCount} dates from {StartDate} to {EndDate}", 
                dates.Count, dates.First().ToString("yyyy-MM-dd"), dates.Last().ToString("yyyy-MM-dd"));

            var random = new Random();
            var shiftsToAdd = new List<Shift>();

            foreach (var date in dates)
            {
                var dayOfWeek = date.DayOfWeek;
                var shiftProbability = GetShiftProbability(dayOfWeek);
                
                // Determine if we have shifts today
                if (random.NextDouble() > shiftProbability)
                {
                    continue; // No shifts today
                }

                // Determine if it's a double (25% of shift days)
                var isDouble = random.NextDouble() < DoubleShiftProbability;
                
                if (isDouble)
                {
                    var doubleShifts = CreateDoubleShift(userId, date, random);
                    shiftsToAdd.AddRange(doubleShifts);
                }
                else
                {
                    var singleShift = CreateSingleShift(userId, date, random);
                    shiftsToAdd.Add(singleShift);
                }
            }

            _logger.LogInformation("Generated {ShiftCount} shifts for {DateCount} dates", shiftsToAdd.Count, dates.Count);

            // Add all shifts to database
            foreach (var shift in shiftsToAdd)
            {
                await _shiftsRepository.AddAsync(shift);
            }
        }

        private static double GetShiftProbability(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday or DayOfWeek.Tuesday => MondayTuesdayShiftProbability,
                DayOfWeek.Wednesday or DayOfWeek.Thursday => WednesdayThursdayShiftProbability,
                DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday => FridaySundayShiftProbability,
                _ => MondayTuesdayShiftProbability
            };
        }

        private Shift CreateSingleShift(string userId, DateTime date, Random random)
        {
            var (startTime, duration) = GenerateShiftTiming(random, false);
            var utcDateTime = _timeZoneService.ConvertToUtc(date, startTime);

            return new Shift
            {
                UserId = userId,
                Date = utcDateTime,
                CreditTips = random.Next(MinimumCreditTips, MaximumCreditTips),
                CashTips = random.Next(MinimumCashTips, MaximumCashTips),
                Tipout = random.Next(MinimumTipout, MaximumTipout),
                HoursWorked = duration
            };
        }

        private List<Shift> CreateDoubleShift(string userId, DateTime date, Random random)
        {
            var shifts = new List<Shift>();
            var (firstStart, firstDuration) = GenerateShiftTiming(random, true);
            
            // Second shift starts at least 1 hour after first ends
            var firstEnd = firstStart.Add(TimeSpan.FromHours(firstDuration));

            // Generate realistic break duration with fractional hours (e.g., 1.5, 2.25 hours) for natural scheduling
            var breakDurationHours = MinimumBreakBetweenShiftsHours
                + random.NextDouble() * (MaximumBreakBetweenShiftsHours - MinimumBreakBetweenShiftsHours);

            var secondStart = firstEnd.Add(TimeSpan.FromHours(breakDurationHours)); // 1-3 hour gap with fractional precision

            // Calculate duration for second shift
            var secondDuration = CalculateSecondShiftDuration(firstDuration, secondStart, random);
            
            // Create first shift
            var firstUtcDateTime = _timeZoneService.ConvertToUtc(date, firstStart);
            shifts.Add(new Shift
            {
                UserId = userId,
                Date = firstUtcDateTime,
                CreditTips = random.Next(MinimumCreditTips, MaximumCreditTips),
                CashTips = random.Next(MinimumCashTips, MaximumCashTips),
                Tipout = random.Next(MinimumTipout, MaximumTipout),
                HoursWorked = firstDuration
            });

            // Create second shift
            var secondUtcDateTime = _timeZoneService.ConvertToUtc(date, secondStart);
            shifts.Add(new Shift
            {
                UserId = userId,
                Date = secondUtcDateTime,
                CreditTips = random.Next(MinimumCreditTips, MaximumCreditTips),
                CashTips = random.Next(MinimumCashTips, MaximumCashTips),
                Tipout = random.Next(MinimumTipout, MaximumTipout),
                HoursWorked = secondDuration
            });

            return shifts;
        }

        private static int CalculateSecondShiftDuration(int firstDuration, TimeSpan secondStart, Random random)
        {
            // Calculate max duration for second shift (total max 12 hours)
            var maxSecondDuration = Math.Min(MaximumSingleShiftDurationHours, MaximumDailyHours - firstDuration);

            var secondDuration = random.Next(
                MinimumShiftDurationHours,
                Math.Max(MinimumShiftDurationHours, maxSecondDuration + 1)
            );

            // Ensure second shift ends before midnight
            var secondEnd = secondStart.Add(TimeSpan.FromHours(secondDuration));
            if (secondEnd >= TimeSpan.FromDays(1))
            {
                // Adjust to end by 11:59 PM
                var maxEndTime = new TimeSpan(MaximumDayHour, 59, 0);
                var adjustedDuration = (int)(maxEndTime - secondStart).TotalHours;
                secondDuration = Math.Max(MinimumShiftDurationHours, adjustedDuration);
            }

            return secondDuration;
        }

        private static (TimeSpan startTime, int duration) GenerateShiftTiming(Random random, bool isFirstOfDouble)
        {
            // Time windows: Morning (8-12), Afternoon (12-16), Evening (16-20)
            var windowChoice = random.Next(0, 3);
            var startHour = windowChoice switch
            {
                0 => random.Next(MorningShiftStartHour, MorningShiftEndHour),   // Morning: 8 AM - 12 PM
                1 => random.Next(AfternoonShiftStartHour, AfternoonShiftEndHour),  // Afternoon: 12 PM - 4 PM  
                2 => random.Next(EveningShiftStartHour, EveningShiftEndHour),  // Evening: 4 PM - 8 PM
                _ => MorningShiftStartHour
            };

            var startMinute = random.Next(0, MinuteIntervalsPerHour) * MinuteIntervals; // 0, 15, 30, or 45 minutes
            var startTime = new TimeSpan(startHour, startMinute, 0);

            // Duration: 3-8 hours for single, 3-6 hours for first of double
            var maxDuration = isFirstOfDouble ? MaximumFirstDoubleShiftDurationHours : MaximumSingleShiftDurationHours;
            var duration = random.Next(MinimumShiftDurationHours, maxDuration + 1);

            // Ensure shift ends before midnight
            var endTime = startTime.Add(TimeSpan.FromHours(duration));
            if (endTime >= TimeSpan.FromDays(1))
            {
                duration = MaximumDayHour - startHour;
                duration = Math.Max(MinimumShiftDurationHours, duration);
            }

            return (startTime, duration);
        }
    }
}
