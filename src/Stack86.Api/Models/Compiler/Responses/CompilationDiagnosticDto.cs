namespace Stack86.Api.Models.Compiler.Responses;

/// <summary>
/// A single diagnostic message (error or warning) from the compiler.
/// </summary>
public sealed record CompilationDiagnosticDto
{
    public required string Message { get; init; }

    public required int Line { get; init; }

    public required int Column { get; init; }

    /// <summary>
    /// The source file that produced this diagnostic, if available.
    /// </summary>
    public string? File { get; init; }
}
