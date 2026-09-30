namespace Stack86.Logic.Compilation.Models;

/// <summary>
/// Result payload returned by the compilation pipeline.
/// </summary>
public sealed record CompilationResultDto
{
    /// <summary>
    /// The generated 8086 assembly, or null if compilation failed.
    /// </summary>
    public string? Assembly { get; init; }

    /// <summary>
    /// Compilation errors.
    /// </summary>
    public IReadOnlyList<DiagnosticDto> Errors { get; init; } = [];

    /// <summary>
    /// Compilation warnings.
    /// </summary>
    public IReadOnlyList<DiagnosticDto> Warnings { get; init; } = [];

    /// <summary>
    /// Informational console messages logged during the compilation pipeline.
    /// </summary>
    public IReadOnlyList<string> ConsoleMessages { get; init; } = [];
}
