namespace Stack86.Logic.Compilation.Stages.Models;

using Stack86.Logic.Languages;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Mutable state carried between pipeline stages. Each stage reads inputs and writes
/// its outputs back into the context for downstream stages.
/// </summary>
public sealed class CompilationContext
{
    /// <summary>
    /// Gets the language of the original source files.
    /// </summary>
    public required SupportedLanguage Language { get; init; }

    /// <summary>
    /// Gets the original project files keyed by filename.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Files { get; init; }

    /// <summary>
    /// Gets or sets the language currently held by <see cref="Source"/>.
    /// Updated by the transpile stage when source is rewritten into a different language.
    /// </summary>
    public SupportedLanguage CurrentLanguage { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current source string (post-preprocessing/transpilation).
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets line mappings from <see cref="Source"/> back to the original files.
    /// </summary>
    public IReadOnlyList<SourceLineMapping> LineMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the set of system headers (e.g. <c>stdio.h</c>) referenced by the source.
    /// </summary>
    public IReadOnlySet<string> SystemHeaders { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets the IR program produced by the lower-to-IR stage.
    /// </summary>
    public IrProgram? Ir { get; set; }

    /// <summary>
    /// Gets or sets the final 8086 assembly text.
    /// </summary>
    public string? Assembly { get; set; }

    /// <summary>
    /// Gets the accumulating diagnostic list (validators add to this; promoted into the IR
    /// program once it exists).
    /// </summary>
    public List<IrDiagnostic> Diagnostics { get; } = [];

    /// <summary>
    /// Gets the human-readable console messages produced as the pipeline runs.
    /// </summary>
    public List<string> ConsoleMessages { get; } = [];

    /// <summary>
    /// Gets a value indicating whether any error-level diagnostic has been recorded.
    /// </summary>
    public bool HasErrors =>
        this.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)
        || (this.Ir?.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error) ?? false);
}
