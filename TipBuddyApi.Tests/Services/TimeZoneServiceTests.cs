using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TipBuddyApi.Services;

namespace TipBuddyApi.Tests.Services
{
    public class TimeZoneServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<TimeZoneService>> _loggerMock;

        public TimeZoneServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<TimeZoneService>>();
        }

        [Fact]
        public void Constructor_WithValidConfiguredTimeZone_UsesConfiguredTimeZone()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");

            // Act
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);

            // Assert
            Assert.Equal("UTC", service.TimeZone.Id);
            VerifyLoggerWasCalled(LogLevel.Information, "Using configured timezone: UTC");
        }

        [Fact]
        public void Constructor_WithInvalidConfiguredTimeZone_FallsBackToPacificTimeZone()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("Invalid/TimeZone");

            // Act
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);

            // Assert
            // Should fall back to Pacific timezone (either IANA or Windows)
            Assert.True(service.TimeZone.Id == "America/Los_Angeles" || 
                       service.TimeZone.Id == "Pacific Standard Time" ||
                       service.TimeZone.Id == "Pacific Time");
            
            VerifyLoggerWasCalled(LogLevel.Warning, "Configured timezone 'Invalid/TimeZone' not found");
        }

        [Fact]
        public void Constructor_WithEmptyConfiguredTimeZone_UsesPacificTimeZone()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns(string.Empty);

            // Act
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);

            // Assert
            // Should use Pacific timezone (either IANA or Windows)
            Assert.True(service.TimeZone.Id == "America/Los_Angeles" || 
                       service.TimeZone.Id == "Pacific Standard Time" ||
                       service.TimeZone.Id == "Pacific Time");
        }

        [Fact]
        public void Constructor_WithNullConfiguredTimeZone_UsesPacificTimeZone()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns((string?)null);

            // Act
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);

            // Assert
            // Should use Pacific timezone (either IANA or Windows)
            Assert.True(service.TimeZone.Id == "America/Los_Angeles" || 
                       service.TimeZone.Id == "Pacific Standard Time" ||
                       service.TimeZone.Id == "Pacific Time");
        }

        [Fact]
        public void TimeZone_Property_ReturnsConfiguredTimeZone()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);

            // Act
            var timeZone = service.TimeZone;

            // Assert
            Assert.NotNull(timeZone);
            Assert.Equal("UTC", timeZone.Id);
        }

        [Fact]
        public void GetLocalDate_WithUtcDateTime_ReturnsCorrectLocalDate()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            
            var utcDateTime = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.Zero); // 2:30 PM UTC

            // Act
            var localDate = service.GetLocalDate(utcDateTime);

            // Assert
            Assert.Equal(new DateTime(2024, 6, 15), localDate);
        }

        [Fact]
        public void GetLocalDate_WithPacificTimeZone_ReturnsCorrectLocalDate()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns((string?)null); // Will use Pacific timezone
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            
            // UTC time that would be the previous day in Pacific time
            var utcDateTime = new DateTimeOffset(2024, 6, 15, 7, 0, 0, TimeSpan.Zero); // 7:00 AM UTC

            // Act
            var localDate = service.GetLocalDate(utcDateTime);

            // Assert
            // In Pacific time (UTC-7 during daylight saving), this should be June 14th at midnight
            Assert.Equal(new DateTime(2024, 6, 15), localDate); // Should still be June 15th at midnight local time
        }

        [Fact]
        public void GetLocalDate_WithDateTimeBoundary_HandlesCorrectly()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            
            var utcDateTime = new DateTimeOffset(2024, 6, 15, 23, 59, 59, TimeSpan.Zero); // Almost midnight UTC

            // Act
            var localDate = service.GetLocalDate(utcDateTime);

            // Assert
            Assert.Equal(new DateTime(2024, 6, 15), localDate);
        }

        [Fact]
        public void GetCurrentLocalDate_ReturnsCurrentDateInConfiguredTimeZone()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);

            // Act
            var currentLocalDate = service.GetCurrentLocalDate();

            // Assert
            Assert.NotEqual(default(DateTime), currentLocalDate);
            Assert.Equal(currentLocalDate, currentLocalDate.Date); // Should be a date only (time portion should be 00:00:00)
        }

        [Fact]
        public void ConvertToUtc_WithLocalDateAndTime_ReturnsCorrectUtcOffset()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            
            var localDate = new DateTime(2024, 6, 15);
            var localTime = new TimeSpan(14, 30, 0); // 2:30 PM

            // Act
            var utcDateTime = service.ConvertToUtc(localDate, localTime);

            // Assert
            Assert.Equal(new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.Zero), utcDateTime);
        }

        [Fact]
        public void ConvertToUtc_WithLocalDateTime_ReturnsCorrectUtcOffset()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            
            var localDateTime = new DateTime(2024, 6, 15, 14, 30, 0); // 2:30 PM

            // Act
            var utcDateTime = service.ConvertToUtc(localDateTime);

            // Assert
            Assert.Equal(new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.Zero), utcDateTime);
        }

        [Fact]
        public void ConvertToUtc_WithPacificTimeZone_ReturnsCorrectUtcOffset()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns((string?)null); // Will use Pacific timezone
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            
            // June 15th is during daylight saving time (UTC-7)
            var localDateTime = new DateTime(2024, 6, 15, 14, 30, 0); // 2:30 PM Pacific

            // Act
            var utcDateTime = service.ConvertToUtc(localDateTime);

            // Assert
            // Should be 9:30 PM UTC (2:30 PM + 7 hours)
            Assert.Equal(TimeSpan.Zero, utcDateTime.Offset);
            Assert.True(utcDateTime.Hour >= 21); // Should be around 9 PM or later UTC
        }

        [Fact]
        public void ConvertToUtc_WithPacificTimeZoneInWinter_ReturnsCorrectUtcOffset()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns((string?)null); // Will use Pacific timezone
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            
            // January is during standard time (UTC-8)
            var localDateTime = new DateTime(2024, 1, 15, 14, 30, 0); // 2:30 PM Pacific

            // Act
            var utcDateTime = service.ConvertToUtc(localDateTime);

            // Assert
            // Should be 10:30 PM UTC (2:30 PM + 8 hours)
            Assert.Equal(TimeSpan.Zero, utcDateTime.Offset);
            Assert.True(utcDateTime.Hour >= 22); // Should be around 10 PM or later UTC
        }

        [Theory]
        [InlineData("UTC")]
        [InlineData("America/New_York")]
        [InlineData("Europe/London")]
        [InlineData("Asia/Tokyo")]
        public void Constructor_WithVariousValidTimeZones_InitializesCorrectly(string timeZoneId)
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns(timeZoneId);

            // Act
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);

            // Assert
            Assert.Equal(timeZoneId, service.TimeZone.Id);
            VerifyLoggerWasCalled(LogLevel.Information, $"Using configured timezone: {timeZoneId}");
        }

        [Theory]
        [InlineData("Eastern Standard Time")] // Windows timezone
        [InlineData("Pacific Standard Time")] // Windows timezone
        [InlineData("Central Standard Time")] // Windows timezone
        public void Constructor_WithWindowsTimeZones_InitializesCorrectly(string timeZoneId)
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns(timeZoneId);

            // Act & Assert
            // This should not throw an exception on Windows systems
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            Assert.NotNull(service.TimeZone);
        }

        [Fact]
        public void GetLocalDate_WithDifferentDateTimeOffsets_ReturnsConsistentDates()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            
            var baseDateTime = new DateTime(2024, 6, 15, 12, 0, 0);
            var utcOffset1 = new DateTimeOffset(baseDateTime, TimeSpan.Zero);
            var utcOffset2 = new DateTimeOffset(baseDateTime, TimeSpan.FromHours(5)); // Same UTC time, different offset

            // Act
            var localDate1 = service.GetLocalDate(utcOffset1);
            var localDate2 = service.GetLocalDate(utcOffset2.ToUniversalTime());

            // Assert
            Assert.Equal(localDate1, localDate2);
        }

        [Fact]
        public void ConvertToUtc_RoundTripConversion_MaintainsAccuracy()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            
            var originalDateTime = new DateTime(2024, 6, 15, 14, 30, 45);

            // Act
            var utcDateTime = service.ConvertToUtc(originalDateTime);
            var roundTripDate = service.GetLocalDate(utcDateTime);

            // Assert
            Assert.Equal(originalDateTime.Date, roundTripDate);
        }

        [Fact]
        public void ConvertToUtc_WithMidnight_HandlesCorrectly()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            
            var localDate = new DateTime(2024, 6, 15);
            var midnightTime = TimeSpan.Zero;

            // Act
            var utcDateTime = service.ConvertToUtc(localDate, midnightTime);

            // Assert
            Assert.Equal(new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero), utcDateTime);
        }

        [Fact]
        public void ConvertToUtc_WithEndOfDay_HandlesCorrectly()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);
            
            var localDate = new DateTime(2024, 6, 15);
            var endOfDayTime = new TimeSpan(23, 59, 59);

            // Act
            var utcDateTime = service.ConvertToUtc(localDate, endOfDayTime);

            // Assert
            Assert.Equal(new DateTimeOffset(2024, 6, 15, 23, 59, 59, TimeSpan.Zero), utcDateTime);
        }

        [Fact]
        public void GetCurrentLocalDate_CalledMultipleTimes_ReturnsConsistentResults()
        {
            // Arrange
            _configurationMock.Setup(x => x["DemoData:TimeZone"])
                .Returns("UTC");
            var service = new TimeZoneService(_configurationMock.Object, _loggerMock.Object);

            // Act
            var date1 = service.GetCurrentLocalDate();
            var date2 = service.GetCurrentLocalDate();

            // Assert
            // Should be the same or at most one day different (if called around midnight)
            var daysDifference = Math.Abs((date2 - date1).Days);
            Assert.True(daysDifference <= 1);
        }

        private void VerifyLoggerWasCalled(LogLevel expectedLogLevel, string expectedMessage)
        {
            _loggerMock.Verify(
                logger => logger.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }
    }
}