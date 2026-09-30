namespace Stack86.Logic.Languages;

using System.Text;
using Stack86.Common.Exceptions;
using Stack86.Logic.Compilation.Stages.Models;

/// <summary>
/// Base <see cref="ILanguagePreprocessor"/> that merges all project files into a single
/// translation unit with the entry-point file emitted first, producing per-line mappings
/// back to the originating file so diagnostics can be remapped. Languages that need to
/// rewrite cross-file declarations (e.g. Go's duplicate <c>package</c> clause) override
/// <see cref="Merge"/>.
/// </summary>
public abstract class MergingLanguagePreprocessor : ILanguagePreprocessor
{
    /// <inheritdoc />
    public abstract SupportedLanguage Language { get; }

    /// <summary>
    /// Gets the ordered list of conventional entry-point file names for this language
    /// (e.g. <c>main.py</c>, <c>Main.java</c>). The first existing candidate is emitted first.
    /// </summary>
    protected abstract IReadOnlyList<string> MainFileCandidates { get; }

    /// <inheritdoc />
    public PreprocessedSource Preprocess(IReadOnlyDictionary<string, string> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        if (files.Count == 0)
        {
            throw new CompilationFailedException("No source files were provided.");
        }

        var mainFile = this.ResolveMainFile(files);

        var ordered = new List<KeyValuePair<string, string>>(files.Count)
        {
            new(mainFile, files[mainFile]),
        };
        ordered.AddRange(files
            .Where(kvp => !string.Equals(kvp.Key, mainFile, StringComparison.Ordinal))
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal));

        return this.Merge(ordered);
    }

    /// <summary>
    /// Appends each line of <paramref name="content"/> to <paramref name="output"/>,
    /// recording a 1-based <see cref="SourceLineMapping"/> per line.
    /// </summary>
    /// <param name="output">The merged-source builder.</param>
    /// <param name="mappings">The accumulating line mappings.</param>
    /// <param name="fileName">The originating file name.</param>
    /// <param name="content">The file content.</param>
    protected static void AppendFile(
        StringBuilder output,
        List<SourceLineMapping> mappings,
        string fileName,
        string content)
    {
        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            output.Append(lines[i]).Append('\n');
            mappings.Add(new SourceLineMapping(fileName, i + 1));
        }
    }

    /// <summary>
    /// Appends every line of <paramref name="orderedFiles"/> verbatim, recording a
    /// <see cref="SourceLineMapping"/> for each emitted line. This is the default
    /// (identity) merge used by languages that tolerate concatenation.
    /// </summary>
    /// <param name="orderedFiles">Project files, entry point first.</param>
    /// <returns>The merged source plus one line mapping per output line.</returns>
    protected virtual PreprocessedSource Merge(IReadOnlyList<KeyValuePair<string, string>> orderedFiles)
    {
        var output = new StringBuilder();
        var mappings = new List<SourceLineMapping>();

        foreach (var (fileName, content) in orderedFiles)
        {
            AppendFile(output, mappings, fileName, content);
        }

        return new PreprocessedSource(
            output.ToString(),
            mappings,
            new HashSet<string>());
    }

    private string ResolveMainFile(IReadOnlyDictionary<string, string> files)
    {
        foreach (var candidate in this.MainFileCandidates)
        {
            if (files.ContainsKey(candidate))
            {
                return candidate;
            }
        }

        return files.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
    }
}
