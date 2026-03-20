using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaidLoop.Core.Contracts;

public sealed class FlexibleDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    private static readonly string[] LegacyUtcFormats =
    [
        "yyyy-MM-dd HH:mm:ss.FFFFFF",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd HH:mm:ss"
    ];

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token when parsing {nameof(DateTimeOffset)}.");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Timestamp string was null or empty.");
        }

        DateTimeOffset parsed;

        if (DateTimeOffset.TryParseExact(
                value,
                LegacyUtcFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsed))
        {
            return parsed;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
        {
            return parsed;
        }

        throw new JsonException($"Unable to parse DateTimeOffset value '{value}'.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
    }
}
