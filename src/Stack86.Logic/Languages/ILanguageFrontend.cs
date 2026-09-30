namespace Stack86.Logic.Languages;

using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Per-language source-to-IR frontend. Each implementation owns the
/// lex → parse → IR-generate chain for its language.
/// </summary>
public interface ILanguageFrontend : ILanguageService
{
    /// <summary>
    /// Lowers the supplied source text to an <see cref="IrProgram"/>.
    /// </summary>
    /// <param name="input">The preprocessed/transpiled source plus its supporting context.</param>
    /// <returns>The lowered IR program.</returns>
    /// <exception cref="Stack86.Common.Exceptions.CompilationFailedException">Thrown when lowering fails.</exception>
    IrProgram Lower(LanguageFrontendInput input);
}
