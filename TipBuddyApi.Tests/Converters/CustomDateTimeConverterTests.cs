using System.Text.Json;
using TipBuddyApi.Converters;

namespace TipBuddyApi.Tests.Converters
{
    public class CustomDateTimeConverterTests
    {
        private readonly JsonSerializerOptions _options;

        public CustomDateTimeConverterTests()
        {
            _options = new JsonSerializerOptions
            {
                Converters = { new CustomDateTimeConverter() }
            };
        }

        [Fact]
        public void Write_SerializesDateTimeToIso8601UtcWithMilliseconds()
        {
            var date = new DateTime(2023, 1, 1, 12, 34, 56, 789, DateTimeKind.Utc);
            var json = JsonSerializer.Serialize(date, _options);

            Assert.Equal("\"2023-01-01T12:34:56.789Z\"", json);
        }

        [Fact]
        public void Read_DeserializesIso8601UtcStringToDateTime()
        {
            var json = "\"2023-01-01T12:34:56.789Z\"";
            var result = JsonSerializer.Deserialize<DateTime>(json, _options);

            Assert.Equal(new DateTime(2023, 1, 1, 12, 34, 56, 789, DateTimeKind.Utc), result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        [Fact]
        public void Read_ThrowsJsonException_WhenNull()
        {
            var json = "null";
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime>(json, _options));
            Assert.Equal("Date string cannot be null.", ex.Message);
        }

        [Fact]
        public void Read_ThrowsFormatException_WhenInvalidFormat()
        {
            var json = "\"not-a-date\"";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime>(json, _options));
        }

        [Fact]
        public void Write_SerializesLocalDateTime_AsUtcWithZ()
        {
            var localDate = new DateTime(2023, 1, 1, 12, 34, 56, 789, DateTimeKind.Local);
            var json = JsonSerializer.Serialize(localDate, _options);

            // Should convert to UTC and append 'Z'
            var expected = $"\"{localDate.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}\"";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void Write_SerializesUnspecifiedKind_AsUtcWithZ()
        {
            var unspecified = new DateTime(2023, 1, 1, 12, 34, 56, 789, DateTimeKind.Unspecified);
            var json = JsonSerializer.Serialize(unspecified, _options);

            // Should treat as local, convert to UTC, and append 'Z'
            var expected = $"\"{unspecified.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}\"";
            Assert.Equal(expected, json);
        }

        [Theory]
        [InlineData("\"2023-01-01T12:34:56Z\"", 2023, 1, 1, 12, 34, 56, 0)]
        [InlineData("\"2023-01-01T12:34:56.7Z\"", 2023, 1, 1, 12, 34, 56, 700)]
        [InlineData("\"2023-01-01T12:34:56.78Z\"", 2023, 1, 1, 12, 34, 56, 780)]
        [InlineData("\"2023-01-01T12:34:56.789+02:00\"", 2023, 1, 1, 10, 34, 56, 789)] // Should convert to UTC
        public void Read_DeserializesVariousIso8601Formats(string json, int y, int mo, int d, int h, int mi, int s, int ms)
        {
            var result = JsonSerializer.Deserialize<DateTime>(json, _options);
            var expected = new DateTime(y, mo, d, h, mi, s, ms, DateTimeKind.Utc);
            Assert.Equal(expected, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        [Fact]
        public void Read_ThrowsJsonException_WhenEmptyString()
        {
            var json = "\"\"";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime>(json, _options));
        }
    }
}