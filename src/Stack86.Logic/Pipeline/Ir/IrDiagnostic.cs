namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// A single diagnostic produced during frontend compilation.
/// </summary>
public sealed record IrDiagnostic
{
    public required DiagnosticSeverity Severity { get; init; }

    public required string Message { get; init; }

    public required int Line { get; init; }

    public required int Column { get; init; }

    /// <summary>
    /// The source file that produced this diagnostic, if available.
    /// </summary>
    public string? SourceFile { get; init; }
}
