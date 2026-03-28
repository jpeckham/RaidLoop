using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaidLoop.Core.Contracts;

public sealed class FlexibleDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    internal static readonly string[] LegacyUtcFormats =
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

        if (TryParseFlexible(value, out var parsed))
        {
            return parsed;
        }

        throw new JsonException($"Unable to parse DateTimeOffset value '{value}'.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
    }

    internal static bool TryParseFlexible(string value, out DateTimeOffset parsed)
    {
        if (DateTimeOffset.TryParseExact(
                value,
                LegacyUtcFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsed))
        {
            return true;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed);
    }
}

public sealed class FlexibleNullableDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token when parsing {nameof(DateTimeOffset)}.");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (FlexibleDateTimeOffsetJsonConverter.TryParseFlexible(value, out var parsed))
        {
            return parsed;
        }

        throw new JsonException($"Unable to parse DateTimeOffset value '{value}'.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
    }
}
