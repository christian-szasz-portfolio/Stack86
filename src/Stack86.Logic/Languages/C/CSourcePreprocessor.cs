namespace Stack86.Logic.Languages.C;

using Stack86.Logic.Compilation.Stages.Models;

/// <summary>
/// C source preprocessor. Picks the entry-point file (<c>main.c</c> if present, otherwise
/// the first file) and delegates to <see cref="CIncludePreprocessor"/> to resolve quoted
/// includes, expand object-like macros, and merge sibling <c>.c</c> files.
/// </summary>
public sealed class CSourcePreprocessor : ILanguagePreprocessor
{
    private const string DefaultMainFile = "main.c";

    /// <inheritdoc />
    public SupportedLanguage Language => SupportedLanguage.C;

    /// <inheritdoc />
    public PreprocessedSource Preprocess(IReadOnlyDictionary<string, string> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        var mainFile = files.ContainsKey(DefaultMainFile)
            ? DefaultMainFile
            : files.Keys.First();

        var ppResult = CIncludePreprocessor.Preprocess(files, mainFile);

        var mappings = ppResult.LineMappings
            .Select(m => new SourceLineMapping(m.File, m.OriginalLine))
            .ToList();

        return new PreprocessedSource(
            ppResult.MergedSource,
            mappings,
            ppResult.SystemHeaders);
    }
}
