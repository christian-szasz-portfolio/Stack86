namespace Stack86.Common.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using Stack86.Common.Json.Converters;

/// <summary>
/// Centralised <see cref="JsonSerializerOptions"/> for the Stack86 backend.
/// camelCase property + dictionary naming, string enums, decimal-tolerant numbers, ISO-8601 UTC timestamps.
/// </summary>
public static class Stack86JsonOptions
{
    /// <summary>Shared, frozen options instance suitable for direct use in serializers.</summary>
    public static readonly JsonSerializerOptions Default = CreateDefault();

    /// <summary>Builds a fresh options instance with all Stack86 conventions applied.</summary>
    public static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApplyTo(options);
        return options;
    }

    /// <summary>Applies Stack86 conventions to an existing <see cref="JsonSerializerOptions"/> instance.</summary>
    /// <param name="options">The options instance to mutate.</param>
    public static void ApplyTo(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.WriteIndented = false;

        AddIfMissing(options.Converters, new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        AddIfMissing(options.Converters, new DecimalConverter());
        AddIfMissing(options.Converters, new NullableDecimalConverter());
        AddIfMissing(options.Converters, new Iso8601DateTimeConverter());
        AddIfMissing(options.Converters, new NullableIso8601DateTimeConverter());
        AddIfMissing(options.Converters, new Iso8601DateTimeOffsetConverter());
        AddIfMissing(options.Converters, new NullableIso8601DateTimeOffsetConverter());
    }

    private static void AddIfMissing(IList<JsonConverter> converters, JsonConverter converter)
    {
        var type = converter.GetType();
        for (int i = 0; i < converters.Count; i++)
        {
            if (converters[i].GetType() == type)
            {
                return;
            }
        }

        converters.Add(converter);
    }
}
