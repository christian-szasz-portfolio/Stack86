namespace Stack86.Logic.Languages;

using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// External (out-of-process or third-party) code validator for a specific language.
/// Currently only C uses this stage (via TCC); other languages skip it.
/// Implementations are resolved by the <see cref="ILanguageRegistry"/>.
/// </summary>
public interface IExternalCodeValidator : ILanguageService
{
    /// <summary>
    /// Validates the given source code and returns any diagnostics (errors and warnings).
    /// </summary>
    /// <param name="sourceCode">The source code to validate.</param>
    /// <param name="cancellationToken">Token used to cancel the (potentially out-of-process) validation.</param>
    /// <returns>A list of diagnostics; empty if validation passes or the validator is unavailable.</returns>
    Task<IReadOnlyList<IrDiagnostic>> ValidateAsync(string sourceCode, CancellationToken cancellationToken = default);
}
