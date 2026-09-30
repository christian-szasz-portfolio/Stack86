namespace Stack86.Common.Json.Converters;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Serialises nullable <see cref="DateTimeOffset"/> as ISO-8601 with millisecond precision, normalised to UTC.
/// </summary>
public sealed class NullableIso8601DateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

    /// <inheritdoc/>
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Cannot convert token {reader.TokenType} to DateTimeOffset?.");
        }

        var text = reader.GetString();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
        }
    }
}
