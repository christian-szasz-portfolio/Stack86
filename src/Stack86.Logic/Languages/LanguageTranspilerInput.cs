namespace Stack86.Logic.Languages;

/// <summary>
/// Inputs supplied to an <see cref="ILanguageTranspiler"/>: the merged source plus
/// the original project files (needed for multi-file transpilation, e.g. C++).
/// </summary>
/// <param name="Source">The source text to transpile.</param>
/// <param name="Files">All project files keyed by filename.</param>
public sealed record LanguageTranspilerInput(
    string Source,
    IReadOnlyDictionary<string, string> Files);
