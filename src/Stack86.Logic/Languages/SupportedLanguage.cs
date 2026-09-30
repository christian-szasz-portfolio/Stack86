namespace Stack86.Logic.Languages;

/// <summary>
/// Enumeration of supported source languages.
/// </summary>
public sealed class SupportedLanguage
{
    public static readonly SupportedLanguage C = new("c");
    public static readonly SupportedLanguage Cpp = new("cpp");
    public static readonly SupportedLanguage CSharp = new("csharp");
    public static readonly SupportedLanguage JavaScript = new("javascript");
    public static readonly SupportedLanguage TypeScript = new("typescript");

    private static readonly Dictionary<string, SupportedLanguage> All = new(StringComparer.OrdinalIgnoreCase)
    {
        [C.Value] = C,
        [Cpp.Value] = Cpp,
        [CSharp.Value] = CSharp,
        [JavaScript.Value] = JavaScript,
        [TypeScript.Value] = TypeScript,
    };

    private SupportedLanguage(string value)
    {
        this.Value = value;
    }

    public string Value { get; }

    public static bool TryFromValue(string value, out SupportedLanguage language)
    {
        return All.TryGetValue(value, out language!);
    }

    public static IReadOnlyCollection<SupportedLanguage> GetAll() => All.Values;

    public override string ToString() => this.Value;
}
