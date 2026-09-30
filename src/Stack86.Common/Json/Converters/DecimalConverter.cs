namespace Stack86.Common.Json.Converters;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Reads <see cref="decimal"/> from JSON numbers or quoted strings, writes as a JSON number.
/// </summary>
public sealed class DecimalConverter : JsonConverter<decimal>
{
    /// <inheritdoc/>
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDecimal(),
            JsonTokenType.String => decimal.Parse(reader.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture),
            _ => throw new JsonException($"Cannot convert token {reader.TokenType} to decimal."),
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteNumberValue(value);
    }
}
