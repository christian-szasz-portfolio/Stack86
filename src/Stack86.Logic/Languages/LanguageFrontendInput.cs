namespace Stack86.Logic.Languages;

/// <summary>
/// Inputs supplied to an <see cref="ILanguageFrontend"/>: the preprocessed/transpiled
/// source plus the original project files (needed by multi-file frontends such as Roslyn)
/// and the system-header set referenced by C source.
/// </summary>
/// <param name="Source">The single-string source to lower.</param>
/// <param name="Files">All project files keyed by filename.</param>
/// <param name="SystemHeaders">Angle-bracket headers referenced by C source (empty for other languages).</param>
public sealed record LanguageFrontendInput(
    string Source,
    IReadOnlyDictionary<string, string> Files,
    IReadOnlySet<string> SystemHeaders);
