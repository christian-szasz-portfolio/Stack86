namespace Stack86.Logic.Compilation.Providers;

using Stack86.Common.Exceptions;
using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Compilation.Pipeline;
using Stack86.Logic.Languages;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Read-side service that compiles source files into 8086 assembly. Composes and runs a
/// default <see cref="CompilationPipeline"/> for the requested language, either as a single
/// aggregated result or as a stream of progress events.
/// </summary>
public sealed class CompilerProvider(ILanguageRegistry registry, IIrValidator irValidator)
{
    /// <summary>
    /// Compiles the supplied project <paramref name="files"/> for the given language and returns
    /// the aggregated result.
    /// </summary>
    /// <param name="language">The source language identifier.</param>
    /// <param name="files">The project files keyed by filename.</param>
    /// <param name="cancellationToken">Token used to cancel the compilation.</param>
    /// <returns>The aggregated compilation result.</returns>
    /// <exception cref="CompilationFailedException">Thrown when the language is unsupported or a stage hard-fails.</exception>
    public Task<CompilationResultDto> CompileAsync(
        string language,
        IReadOnlyDictionary<string, string> files,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(files);

        if (!SupportedLanguage.TryFromValue(language, out var resolved))
        {
            throw new CompilationFailedException($"Unsupported language: '{language}'.");
        }

        return this.BuildPipeline(resolved, files).RunAsync(cancellationToken);
    }

    /// <summary>
    /// Compiles the supplied project <paramref name="files"/> for the given language, yielding a
    /// stream of progress events terminated by a final completion event.
    /// </summary>
    /// <param name="language">The resolved source language.</param>
    /// <param name="files">The project files keyed by filename.</param>
    /// <param name="cancellationToken">Token used to cancel the stream.</param>
    /// <returns>An asynchronous stream of compile events.</returns>
    public IAsyncEnumerable<CompileStreamEvent> CompileStreamAsync(
        SupportedLanguage language,
        IReadOnlyDictionary<string, string> files,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(files);

        return this.BuildPipeline(language, files).RunStreamingAsync(cancellationToken);
    }

    private CompilationPipeline BuildPipeline(SupportedLanguage language, IReadOnlyDictionary<string, string> files) =>
        CompilationPipeline
            .For(registry, irValidator, language, files)
            .UseDefaultsFor(language);
}
