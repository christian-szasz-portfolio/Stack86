namespace Stack86.Logic.Languages.CSharp;

using Stack86.Logic.Pipeline;

/// <summary>
/// Validates that C# source code uses only types supported by the 8086 target.
/// Rejects <c>float</c>, <c>double</c>, <c>decimal</c>, <c>long</c>, and <c>ulong</c>
/// declarations that would produce incorrect results on a 16-bit target.
/// </summary>
public sealed class CSharpCapabilityValidator(ITargetCapabilityProfile profile)
    : KeywordCapabilityValidator(profile)
{
    private readonly IReadOnlyDictionary<string, string> unsupportedTypeKeywords =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["float"] = $"Type 'float' is not supported by the {profile.TargetName} target. Use 'int' for integer arithmetic.",
            ["double"] = $"Type 'double' is not supported by the {profile.TargetName} target. Use 'int' for integer arithmetic.",
            ["decimal"] = $"Type 'decimal' is not supported by the {profile.TargetName} target. Use 'int' for integer arithmetic.",
            ["long"] = $"Type 'long' is not supported by the {profile.TargetName} target. All integers are 16-bit ({profile.MinIntegerValue}..{profile.MaxIntegerValue}).",
            ["ulong"] = $"Type 'ulong' is not supported by the {profile.TargetName} target. All integers are 16-bit ({profile.MinIntegerValue}..{profile.MaxIntegerValue}).",
            ["uint"] = $"Type 'uint' is not supported by the {profile.TargetName} target. All integers are 16-bit ({profile.MinIntegerValue}..{profile.MaxIntegerValue}).",
            ["short"] = $"Type 'short' is not supported by the {profile.TargetName} target. Use 'int' for integer arithmetic.",
            ["ushort"] = $"Type 'ushort' is not supported by the {profile.TargetName} target. Use 'int' for integer arithmetic.",
            ["byte"] = $"Type 'byte' is not supported by the {profile.TargetName} target. Use 'int' or 'char' for 8-bit values.",
            ["sbyte"] = $"Type 'sbyte' is not supported by the {profile.TargetName} target. Use 'int' or 'char' for 8-bit values.",
        };

    /// <inheritdoc />
    public override SupportedLanguage Language => SupportedLanguage.CSharp;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> UnsupportedTypeKeywords => this.unsupportedTypeKeywords;
}
