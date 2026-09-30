namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// A single three-address-code instruction.
/// </summary>
public sealed record IrInstruction
{
    public required IrOpCode OpCode { get; init; }

    /// <summary>
    /// Destination operand (result of the operation), or null for void instructions.
    /// </summary>
    public IrOperand? Dest { get; init; }

    /// <summary>
    /// First source operand.
    /// </summary>
    public IrOperand? Left { get; init; }

    /// <summary>
    /// Second source operand.
    /// </summary>
    public IrOperand? Right { get; init; }

    /// <summary>
    /// Source line this instruction originated from (for diagnostics).
    /// </summary>
    public int SourceLine { get; init; }
}
