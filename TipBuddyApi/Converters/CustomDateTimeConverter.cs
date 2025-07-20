using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TipBuddyApi.Converters
{
    /// <summary>
    /// Provides custom JSON serialization and deserialization for <see cref="DateTime"/> values.
    /// </summary>
    /// <remarks>This converter serializes <see cref="DateTime"/> values to the ISO 8601 format with
    /// millisecond precision and a "Z" suffix to indicate UTC (e.g., "2023-01-01T12:34:56.789Z"). During
    /// deserialization, it parses date strings and converts them to UTC.</remarks>
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();

            return dateString is null
                ? throw new JsonException("Date string cannot be null.")
                : DateTime.Parse(dateString).ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }
}