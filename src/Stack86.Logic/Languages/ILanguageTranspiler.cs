namespace Stack86.Logic.Languages;

using Stack86.Logic.Compilation.Stages.Models;

/// <summary>
/// Per-language transpiler that rewrites source from one language into another
/// (e.g. C++ → C, TypeScript → JavaScript). Implementations are resolved by the
/// <see cref="ILanguageRegistry"/>; languages without a registered transpiler
/// pass through unchanged.
/// </summary>
public interface ILanguageTranspiler : ILanguageService
{
    /// <summary>
    /// Transpiles <paramref name="input"/> into a more primitive language for
    /// downstream lowering.
    /// </summary>
    /// <param name="input">The source language input plus its supporting context.</param>
    /// <param name="cancellationToken">Token used to cancel the (potentially out-of-process) transpile.</param>
    /// <returns>The transpiled source.</returns>
    /// <exception cref="Stack86.Common.Exceptions.CompilationFailedException">Thrown when transpilation fails.</exception>
    Task<TranspiledSource> TranspileAsync(LanguageTranspilerInput input, CancellationToken cancellationToken = default);
}
