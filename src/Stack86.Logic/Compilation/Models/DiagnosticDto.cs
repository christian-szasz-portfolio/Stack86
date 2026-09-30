namespace Stack86.Logic.Compilation.Models;

/// <summary>
/// Represents a single diagnostic (error or warning) produced during compilation.
/// </summary>
public sealed record DiagnosticDto
{
    public required string Message { get; init; }

    public required int Line { get; init; }

    public required int Column { get; init; }

    /// <summary>
    /// The source file that produced this diagnostic, if available.
    /// </summary>
    public string? SourceFile { get; init; }
}
