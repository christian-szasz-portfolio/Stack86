namespace Stack86.Logic.Languages.TypeScript;

using Stack86.Logic.Pipeline;

/// <summary>
/// Capability validator for TypeScript. Flags floating-point type annotations
/// that would be silently discarded during transpilation to JavaScript.
/// </summary>
public sealed class TsCapabilityValidator(ITargetCapabilityProfile profile)
    : KeywordCapabilityValidator(profile)
{
    private readonly IReadOnlyDictionary<string, string> unsupportedTypeKeywords =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["parseFloat"] = $"'parseFloat' is not supported by the {profile.TargetName} target. All numbers are 16-bit integers ({profile.MinIntegerValue}..{profile.MaxIntegerValue}).",
            ["BigInt"] = $"'BigInt' is not supported by the {profile.TargetName} target. All numbers are 16-bit integers ({profile.MinIntegerValue}..{profile.MaxIntegerValue}).",
        };

    /// <inheritdoc />
    public override SupportedLanguage Language => SupportedLanguage.TypeScript;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> UnsupportedTypeKeywords => this.unsupportedTypeKeywords;
}
