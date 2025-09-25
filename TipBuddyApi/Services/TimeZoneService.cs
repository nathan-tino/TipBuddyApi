using TipBuddyApi.Contracts;

namespace TipBuddyApi.Services
{
    public class TimeZoneService : ITimeZoneService
    {
        private readonly ILogger<TimeZoneService> _logger;
        private readonly TimeZoneInfo _timeZone;

        public TimeZoneInfo TimeZone => _timeZone;

        public TimeZoneService(IConfiguration configuration, ILogger<TimeZoneService> logger)
        {
            _logger = logger;
            _timeZone = GetConfiguredTimeZone(configuration);
        }

        public DateTime GetLocalDate(DateTimeOffset utcDateTime)
        {
            var localDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime.UtcDateTime, _timeZone);
            return localDateTime.Date;
        }

        public DateTime GetCurrentLocalDate()
        {
            return GetLocalDate(DateTimeOffset.UtcNow);
        }

        public DateTimeOffset ConvertToUtc(DateTime localDate, TimeSpan localTime)
        {
            var localDateTime = localDate + localTime;
            return ConvertToUtc(localDateTime);
        }

        public DateTimeOffset ConvertToUtc(DateTime localDateTime)
        {
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, _timeZone);
            return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
        }

        private TimeZoneInfo GetConfiguredTimeZone(IConfiguration configuration)
        {
            var configuredTimeZone = configuration["DemoData:TimeZone"];
            
            if (!string.IsNullOrEmpty(configuredTimeZone))
            {
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(configuredTimeZone);
                    _logger.LogInformation("Using configured timezone: {TimeZone}", configuredTimeZone);
                    return timeZone;
                }
                catch (TimeZoneNotFoundException ex)
                {
                    _logger.LogWarning(ex, "Configured timezone '{ConfiguredTimeZone}' not found, falling back to Pacific timezone", configuredTimeZone);
                }
            }
            
            return GetPacificTimeZone();
        }

        private TimeZoneInfo GetPacificTimeZone()
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
                _logger.LogInformation("Using IANA Pacific timezone: America/Los_Angeles");
                return timeZone;
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                    _logger.LogInformation("Using Windows Pacific timezone: Pacific Standard Time");
                    return timeZone;
                }
                catch (TimeZoneNotFoundException)
                {
                    _logger.LogWarning("Neither IANA nor Windows Pacific timezone found, creating custom Pacific timezone");
                    return TimeZoneInfo.CreateCustomTimeZone(
                        "Pacific Time",
                        TimeSpan.FromHours(-8),
                        "Pacific Time",
                        "Pacific Standard Time",
                        "Pacific Daylight Time",
                        new TimeZoneInfo.AdjustmentRule[]
                        {
                            TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                                DateTime.MinValue.Date,
                                DateTime.MaxValue.Date,
                                TimeSpan.FromHours(1),
                                TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday),
                                TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday))
                        });
                }
            }
        }
    }
}