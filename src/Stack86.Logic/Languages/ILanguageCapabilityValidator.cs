namespace Stack86.Logic.Languages;

using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Validates that source code uses only features supported by the compilation target.
/// Each language frontend has its own implementation that understands the language's
/// type keywords and syntax patterns.
/// </summary>
public interface ILanguageCapabilityValidator : ILanguageService
{
    /// <summary>
    /// Scans the source code for unsupported language features and returns diagnostics.
    /// </summary>
    /// <param name="source">The source code to validate.</param>
    /// <returns>Diagnostics for any unsupported features found; empty if all features are supported.</returns>
    IReadOnlyList<IrDiagnostic> Validate(string source);
}
