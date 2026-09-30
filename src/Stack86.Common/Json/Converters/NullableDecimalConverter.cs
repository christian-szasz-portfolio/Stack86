namespace Stack86.Common.Json.Converters;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Reads nullable <see cref="decimal"/> from JSON numbers, quoted strings, or null. Writes as a JSON number or null.
/// </summary>
public sealed class NullableDecimalConverter : JsonConverter<decimal?>
{
    /// <inheritdoc/>
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number => reader.GetDecimal(),
            JsonTokenType.String => string.IsNullOrEmpty(reader.GetString())
                ? null
                : decimal.Parse(reader.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture),
            _ => throw new JsonException($"Cannot convert token {reader.TokenType} to decimal?."),
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
