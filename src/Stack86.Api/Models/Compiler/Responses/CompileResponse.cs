namespace Stack86.Api.Models.Compiler.Responses;

/// <summary>
/// Response payload from the compile endpoint.
/// </summary>
public sealed record CompileResponse
{
    /// <summary>
    /// Gets the generated 8086 assembly code, or null if compilation failed.
    /// </summary>
    public string? Assembly { get; init; }

    /// <summary>
    /// Gets the compilation errors, if any.
    /// </summary>
    public IReadOnlyList<CompilationDiagnosticDto> Errors { get; init; } = [];

    /// <summary>
    /// Gets the compilation warnings, if any.
    /// </summary>
    public IReadOnlyList<CompilationDiagnosticDto> Warnings { get; init; } = [];

    /// <summary>
    /// Gets the informational console messages logged during the compilation pipeline.
    /// </summary>
    public IReadOnlyList<string> ConsoleMessages { get; init; } = [];
}
