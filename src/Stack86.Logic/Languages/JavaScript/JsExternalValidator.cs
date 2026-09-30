namespace Stack86.Logic.Languages.JavaScript;

using System.Text.RegularExpressions;
using Stack86.Logic.Compilation.Models;

/// <summary>
/// Validates JavaScript source syntax via <c>node --check</c>, which parses the file and
/// reports syntax errors without executing it. Degrades to a warning when <c>node</c> is
/// not installed.
/// </summary>
public sealed partial class JsExternalValidator(NodeCheckSettings settings) : ExternalProcessCodeValidator(settings)
{
    /// <inheritdoc />
    public override SupportedLanguage Language => SupportedLanguage.JavaScript;

    /// <inheritdoc />
    protected override string ToolName => "node";

    /// <inheritdoc />
    protected override string FileExtension => ".js";

    /// <inheritdoc />
    protected override string BuildArguments(string sourceFilePath, string workingDirectory, string nullDevice)
        => $"--check \"{sourceFilePath}\"";

    /// <inheritdoc />
    protected override Regex? DiagnosticLineRegex() => NodeDiagnosticRegex();

    [GeneratedRegex(@":(?<line>\d+)$|SyntaxError:\s*(?<message>.+)$")]
    private static partial Regex NodeDiagnosticRegex();
}
