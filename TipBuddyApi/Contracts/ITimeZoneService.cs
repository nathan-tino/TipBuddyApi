namespace TipBuddyApi.Contracts
{
    /// <summary>
    /// Provides timezone-aware date and time operations for the application.
    /// Handles timezone configuration, conversion between UTC and local time,
    /// and provides methods for working with dates in the configured timezone.
    /// </summary>
    public interface ITimeZoneService
    {
        /// <summary>
        /// Gets the configured timezone for the application.
        /// </summary>
        TimeZoneInfo TimeZone { get; }

        /// <summary>
        /// Converts a UTC DateTimeOffset to a date in the configured timezone.
        /// </summary>
        /// <param name="utcDateTime">The UTC datetime to convert</param>
        /// <returns>The date portion in the configured timezone</returns>
        DateTime GetLocalDate(DateTimeOffset utcDateTime);

        /// <summary>
        /// Gets the current date in the configured timezone.
        /// </summary>
        /// <returns>Today's date in the configured timezone</returns>
        DateTime GetCurrentLocalDate();

        /// <summary>
        /// Converts a local date and time to UTC DateTimeOffset.
        /// </summary>
        /// <param name="localDate">The local date</param>
        /// <param name="localTime">The local time</param>
        /// <returns>The UTC DateTimeOffset</returns>
        DateTimeOffset ConvertToUtc(DateTime localDate, TimeSpan localTime);

        /// <summary>
        /// Converts a local DateTime to UTC DateTimeOffset.
        /// </summary>
        /// <param name="localDateTime">The local datetime</param>
        /// <returns>The UTC DateTimeOffset</returns>
        DateTimeOffset ConvertToUtc(DateTime localDateTime);
    }
}