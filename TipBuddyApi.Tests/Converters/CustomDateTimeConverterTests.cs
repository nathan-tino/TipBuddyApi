using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TipBuddyApi.Converters;
using Xunit;

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
}