namespace Stack86.Common.Json.Converters;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Serialises nullable <see cref="DateTime"/> as ISO-8601 with millisecond precision, normalised to UTC.
/// </summary>
public sealed class NullableIso8601DateTimeConverter : JsonConverter<DateTime?>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

    /// <inheritdoc/>
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Cannot convert token {reader.TokenType} to DateTime?.");
        }

        var text = reader.GetString();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var utc = value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime();
        writer.WriteStringValue(utc.ToString(Format, CultureInfo.InvariantCulture));
    }
}
