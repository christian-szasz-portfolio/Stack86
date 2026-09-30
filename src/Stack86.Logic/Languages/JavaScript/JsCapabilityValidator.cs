namespace Stack86.Logic.Languages.JavaScript;

using System.Text.RegularExpressions;
using Stack86.Logic.Pipeline;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Capability validator for JavaScript. Since JS is dynamically typed, source-level
/// validation is minimal — only known unsupported global functions and module imports that do
/// not resolve to a project-local file are flagged. Intra-project ES module imports are resolved
/// and stripped by <see cref="JsSourcePreprocessor"/> before this stage runs, so any import that
/// reaches the validator references an unavailable external module. The IR-level validator
/// provides the main safety net for JS programs.
/// </summary>
public sealed partial class JsCapabilityValidator(ITargetCapabilityProfile profile)
    : KeywordCapabilityValidator(profile)
{
    private readonly IReadOnlyDictionary<string, string> unsupportedTypeKeywords =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["parseFloat"] = $"'parseFloat' is not supported by the {profile.TargetName} target. All numbers are 16-bit integers ({profile.MinIntegerValue}..{profile.MaxIntegerValue}).",
            ["BigInt"] = $"'BigInt' is not supported by the {profile.TargetName} target. All numbers are 16-bit integers ({profile.MinIntegerValue}..{profile.MaxIntegerValue}).",
        };

    /// <inheritdoc />
    public override SupportedLanguage Language => SupportedLanguage.JavaScript;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> UnsupportedTypeKeywords => this.unsupportedTypeKeywords;

    /// <inheritdoc />
    public override IReadOnlyList<IrDiagnostic> Validate(string source)
    {
        var diagnostics = new List<IrDiagnostic>(base.Validate(source));
        var lines = source.Split('\n');

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var specifier = TryGetUnresolvedSpecifier(lines[lineIndex]);
            if (specifier is not null)
            {
                diagnostics.Add(new IrDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Module '{specifier}' is not available on the {this.Profile.TargetName} target. Only project-local modules can be imported.",
                    Line = lineIndex + 1,
                    Column = 1,
                });
            }
        }

        return diagnostics;
    }

    private static string? TryGetUnresolvedSpecifier(string line)
    {
        var fromMatch = ImportFromRegex().Match(line);
        if (fromMatch.Success)
        {
            return fromMatch.Groups["spec"].Value;
        }

        var sideEffectMatch = SideEffectImportRegex().Match(line);
        return sideEffectMatch.Success ? sideEffectMatch.Groups["spec"].Value : null;
    }

    [GeneratedRegex(@"^\s*import\s+.+?\s+from\s+['""](?<spec>[^'""]+)['""]\s*;?\s*$")]
    private static partial Regex ImportFromRegex();

    [GeneratedRegex(@"^\s*import\s+['""](?<spec>[^'""]+)['""]\s*;?\s*$")]
    private static partial Regex SideEffectImportRegex();
}
