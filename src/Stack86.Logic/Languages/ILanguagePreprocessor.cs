namespace Stack86.Logic.Languages;

using Stack86.Logic.Compilation.Stages.Models;

/// <summary>
/// Per-language source preprocessor (e.g. C <c>#include</c> resolution).
/// Languages without a registered preprocessor fall back to a verbatim
/// concatenation of all project files.
/// </summary>
public interface ILanguagePreprocessor : ILanguageService
{
    /// <summary>
    /// Resolves any include/import directives across <paramref name="files"/> and
    /// returns the merged source plus per-line origin mappings.
    /// </summary>
    /// <param name="files">All project files keyed by filename.</param>
    /// <returns>The preprocessed source.</returns>
    /// <exception cref="Stack86.Common.Exceptions.CompilationFailedException">Thrown when preprocessing fails.</exception>
    PreprocessedSource Preprocess(IReadOnlyDictionary<string, string> files);
}
