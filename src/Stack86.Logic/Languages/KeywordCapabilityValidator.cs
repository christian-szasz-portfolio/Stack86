namespace Stack86.Logic.Languages;

using System.Text.RegularExpressions;
using Stack86.Logic.Pipeline;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Base class for keyword-based capability validators. Scans source lines for
/// unsupported type keywords and emits clear diagnostics instead of allowing
/// the parser to crash or silently downgrade types.
/// </summary>
public abstract partial class KeywordCapabilityValidator(ITargetCapabilityProfile profile) : ILanguageCapabilityValidator
{
    /// <inheritdoc />
    public abstract SupportedLanguage Language { get; }

    /// <summary>
    /// Gets the mapping of unsupported type keywords to their diagnostic messages.
    /// Keys are the keyword strings; values are the error messages to emit.
    /// </summary>
    protected abstract IReadOnlyDictionary<string, string> UnsupportedTypeKeywords { get; }

    /// <summary>
    /// Gets the target capability profile.
    /// </summary>
    protected ITargetCapabilityProfile Profile => profile;

    /// <inheritdoc />
    public virtual IReadOnlyList<IrDiagnostic> Validate(string source)
    {
        var diagnostics = new List<IrDiagnostic>();
        var lines = source.Split('\n');

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = StripStringContent(lines[lineIndex]);

            foreach (var match in WordRegex().EnumerateMatches(line))
            {
                var word = line.AsSpan(match.Index, match.Length).ToString();

                if (this.UnsupportedTypeKeywords.TryGetValue(word, out var message))
                {
                    diagnostics.Add(new IrDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = message,
                        Line = lineIndex + 1,
                        Column = match.Index + 1,
                    });
                }
            }
        }

        return diagnostics;
    }

    /// <summary>
    /// Blanks out the content of string literals and single-line comments so keyword
    /// scanning does not produce false positives.
    /// </summary>
    internal static string StripStringContent(string line)
    {
        return StringAndCommentRegex().Replace(line, match =>
        {
            if (match.Value.StartsWith("//", StringComparison.Ordinal))
            {
                return new string(' ', match.Length);
            }

            if (match.Value.StartsWith("/*", StringComparison.Ordinal))
            {
                return new string(' ', match.Length);
            }

            return match.Value[0] + new string(' ', match.Length - 2) + match.Value[^1];
        });
    }

    [GeneratedRegex(@"\b[A-Za-z_]\w*\b")]
    private static partial Regex WordRegex();

    [GeneratedRegex("""//.*|/\*.*?\*/|"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'""")]
    private static partial Regex StringAndCommentRegex();
}
