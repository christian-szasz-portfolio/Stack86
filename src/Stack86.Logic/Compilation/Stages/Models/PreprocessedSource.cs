namespace Stack86.Logic.Compilation.Stages.Models;

/// <summary>
/// Output of the preprocess stage: a single merged source string plus optional line
/// mappings back to the original files and the set of resolved system headers.
/// </summary>
public sealed record PreprocessedSource(
    string Source,
    IReadOnlyList<SourceLineMapping> LineMappings,
    IReadOnlySet<string> SystemHeaders);
