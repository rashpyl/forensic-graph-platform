using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ForensicGraph.Api.Serialization;

/// <summary>
/// Serializes <see cref="DateTime"/> as ISO-8601 UTC ending in 'Z'.
/// Any non-UTC value is converted to UTC before writing so the API surface
/// never leaks local-time ambiguity to clients.
/// On read, values are parsed as UTC (round-trip 'O' format).
/// </summary>
internal sealed class Iso8601UtcDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString()
                  ?? throw new JsonException("Expected non-null datetime string.");
        var parsed = DateTime.Parse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal);
        return parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
        writer.WriteStringValue(utc.ToString(Format, CultureInfo.InvariantCulture));
    }
}
