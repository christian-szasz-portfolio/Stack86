namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// The complete IR program output by a language frontend — a collection of functions plus diagnostics.
/// </summary>
public sealed record IrProgram
{
    /// <summary>
    /// All functions in the program, in definition order.
    /// </summary>
    public required IReadOnlyList<IrFunction> Functions { get; init; }

    /// <summary>
    /// Diagnostics emitted during frontend processing (warnings + errors).
    /// </summary>
    public IReadOnlyList<IrDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Global data (string literals, constant arrays, etc.) to embed in the data segment.
    /// </summary>
    public IReadOnlyList<IrGlobalData> Globals { get; init; } = [];

    /// <summary>
    /// Struct type definitions to emit as STRUC/ENDS directives in the assembly output.
    /// </summary>
    public IReadOnlyList<IrStructDefinition> StructDefinitions { get; init; } = [];

    /// <summary>
    /// Virtual method tables to be emitted in the data segment.
    /// </summary>
    public IReadOnlyList<IrVtableEntry> Vtables { get; init; } = [];
}
