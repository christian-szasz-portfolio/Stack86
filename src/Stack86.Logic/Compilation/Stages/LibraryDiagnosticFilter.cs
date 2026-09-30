namespace Stack86.Logic.Compilation.Stages;

using System.Text.RegularExpressions;
using Stack86.Logic.Languages.C;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Filters known false-positive library-call warnings emitted by external validators
/// (TCC does not ship system headers and warns on every libc call).
/// </summary>
internal static partial class LibraryDiagnosticFilter
{
    /// <summary>
    /// Removes "implicit declaration of function" warnings for functions that are
    /// registered in <see cref="CLibraryRegistry"/> and whose required header has been included.
    /// </summary>
    public static IReadOnlyList<IrDiagnostic> FilterKnownLibraryWarnings(
        IReadOnlyList<IrDiagnostic> diagnostics,
        IReadOnlySet<string> systemHeaders)
    {
        return [.. diagnostics.Where(d =>
        {
            if (d.Severity != DiagnosticSeverity.Warning)
            {
                return true;
            }

            var match = ImplicitDeclarationRegex().Match(d.Message);
            if (!match.Success)
            {
                return true;
            }

            var functionName = match.Groups["name"].Value;
            if (CLibraryRegistry.TryGetFunction(functionName, out var libFunc)
                && CLibraryRegistry.IsHeaderIncluded(libFunc, systemHeaders))
            {
                return false;
            }

            return true;
        })];
    }

    [GeneratedRegex(@"implicit declaration of function\s+[`'\u2018](?<name>[^'`\u2019]+)['\u2019]", RegexOptions.IgnoreCase)]
    private static partial Regex ImplicitDeclarationRegex();
}
