namespace Stack86.Logic.Languages.C;

using System.Text.RegularExpressions;
using Stack86.Logic.Pipeline;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Scans C source text for type keywords and qualifiers that the compilation target
/// does not support (e.g. <c>float</c>, <c>double</c>, <c>long</c>, <c>const</c>).
/// Runs after TCC validation but before the internal lexer/parser so the user
/// receives a clear, actionable diagnostic instead of a cryptic parse failure.
/// </summary>
public sealed partial class CCapabilityValidator(ITargetCapabilityProfile profile) : ILanguageCapabilityValidator
{
    /// <summary>
    /// Set of C keywords that indicate a <c>goto</c> statement, which is unsupported.
    /// </summary>
    private static readonly HashSet<string> UnsupportedStatementKeywords = new(StringComparer.Ordinal)
    {
        "goto",
    };

    /// <inheritdoc />
    public SupportedLanguage Language => SupportedLanguage.C;

    /// <inheritdoc />
    public IReadOnlyList<IrDiagnostic> Validate(string source)
    {
        var diagnostics = new List<IrDiagnostic>();
        var lines = source.Split('\n');
        var inBlockComment = false;

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = StripCommentAndStringContent(lines[lineIndex], ref inBlockComment);

            // Skip preprocessor directives — they are consumed by the preprocessor/TCC
            if (line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            foreach (var match in WordRegex().EnumerateMatches(line))
            {
                var word = line.AsSpan(match.Index, match.Length).ToString();

                if (profile.UnsupportedCTypeKeywords.Contains(word))
                {
                    diagnostics.Add(new IrDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Type '{word}' is not supported by the {profile.TargetName} target. Only 'int', 'char', and 'void' are supported.",
                        Line = lineIndex + 1,
                        Column = match.Index + 1,
                    });
                }
                else if (profile.UnsupportedCQualifierKeywords.Contains(word))
                {
                    diagnostics.Add(new IrDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Qualifier '{word}' is not supported by the {profile.TargetName} target.",
                        Line = lineIndex + 1,
                        Column = match.Index + 1,
                    });
                }
                else if (UnsupportedStatementKeywords.Contains(word))
                {
                    diagnostics.Add(new IrDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = $"'{word}' statements are not supported by the {profile.TargetName} target.",
                        Line = lineIndex + 1,
                        Column = match.Index + 1,
                    });
                }
            }
        }

        return diagnostics;
    }

    /// <summary>
    /// Strips the content of string/char literals and comments (including block comments
    /// spanning multiple lines) so keyword scanning does not produce false positives for
    /// words inside strings or comments. Column positions are preserved by replacing the
    /// stripped content with spaces.
    /// </summary>
    /// <param name="line">The source line to strip.</param>
    /// <param name="inBlockComment">
    /// Tracks whether the scan begins inside an unterminated block comment carried over from
    /// a previous line; updated to reflect the state at the end of this line.
    /// </param>
    /// <returns>The line with comment and literal content blanked out.</returns>
    internal static string StripCommentAndStringContent(string line, ref bool inBlockComment)
    {
        var chars = line.ToCharArray();
        var i = 0;

        while (i < chars.Length)
        {
            if (inBlockComment)
            {
                if (i + 1 < chars.Length && chars[i] == '*' && chars[i + 1] == '/')
                {
                    chars[i] = ' ';
                    chars[i + 1] = ' ';
                    i += 2;
                    inBlockComment = false;
                }
                else
                {
                    chars[i] = ' ';
                    i++;
                }

                continue;
            }

            // Line comment — blank to end of line.
            if (chars[i] == '/' && i + 1 < chars.Length && chars[i + 1] == '/')
            {
                for (var j = i; j < chars.Length; j++)
                {
                    chars[j] = ' ';
                }

                break;
            }

            // Block comment start — blank and switch into block-comment state.
            if (chars[i] == '/' && i + 1 < chars.Length && chars[i + 1] == '*')
            {
                chars[i] = ' ';
                chars[i + 1] = ' ';
                i += 2;
                inBlockComment = true;
                continue;
            }

            // String or char literal — preserve delimiters, blank the interior.
            if (chars[i] == '"' || chars[i] == '\'')
            {
                var quote = chars[i];
                i++;
                while (i < chars.Length && chars[i] != quote)
                {
                    if (chars[i] == '\\' && i + 1 < chars.Length)
                    {
                        chars[i] = ' ';
                        i++;
                    }

                    chars[i] = ' ';
                    i++;
                }

                if (i < chars.Length)
                {
                    i++;
                }

                continue;
            }

            i++;
        }

        return new string(chars);
    }

    /// <summary>
    /// Matches C identifier words (keyword candidates).
    /// </summary>
    [GeneratedRegex(@"\b[A-Za-z_]\w*\b")]
    private static partial Regex WordRegex();
}
