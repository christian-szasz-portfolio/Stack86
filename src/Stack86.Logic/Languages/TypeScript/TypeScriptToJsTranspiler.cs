namespace Stack86.Logic.Languages.TypeScript;

using Stack86.Common.Exceptions;
using Stack86.Logic.Compilation.Stages.Models;

/// <summary>
/// TypeScript → JavaScript transpiler. Wraps the configured
/// <see cref="ITypeScriptTranspiler"/> (Node.js child process in production,
/// identity transform in tests) and applies <see cref="TsEnumInliner"/> to the result.
/// </summary>
public sealed class TypeScriptToJsTranspiler(ITypeScriptTranspiler tsTranspiler) : ILanguageTranspiler
{
    /// <inheritdoc />
    public SupportedLanguage Language => SupportedLanguage.TypeScript;

    /// <inheritdoc />
    public async Task<TranspiledSource> TranspileAsync(LanguageTranspilerInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        try
        {
            var jsSource = await tsTranspiler.TranspileAsync(input.Source, cancellationToken);
            jsSource = TsEnumInliner.InlineEnums(jsSource);
            return new TranspiledSource(SupportedLanguage.JavaScript, jsSource, []);
        }
        catch (ExternalToolException ex)
        {
            throw new CompilationFailedException(ex.Message);
        }
    }
}
