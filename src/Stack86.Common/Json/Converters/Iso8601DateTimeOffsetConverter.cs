namespace Stack86.Common.Json.Converters;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Serialises <see cref="DateTimeOffset"/> as ISO-8601 with millisecond precision, normalised to UTC.
/// </summary>
public sealed class Iso8601DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

    /// <inheritdoc/>
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Cannot convert token {reader.TokenType} to DateTimeOffset.");
        }

        var text = reader.GetString();
        if (string.IsNullOrEmpty(text))
        {
            throw new JsonException("Empty DateTimeOffset string.");
        }

        return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
    }
}
