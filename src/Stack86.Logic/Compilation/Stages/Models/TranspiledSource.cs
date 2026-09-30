namespace Stack86.Logic.Compilation.Stages.Models;

using Stack86.Logic.Languages;

/// <summary>
/// Output of the transpile stage: source rewritten into a target language plus
/// optional line mappings back to the original source.
/// </summary>
public sealed record TranspiledSource(
    SupportedLanguage TargetLanguage,
    string Source,
    IReadOnlyList<SourceLineMapping> LineMappings);
