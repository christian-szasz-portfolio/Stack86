namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// A single operand in a three-address IR instruction.
/// </summary>
public sealed record IrOperand
{
    public required IrOperandKind Kind { get; init; }

    /// <summary>
    /// Virtual register index (when <see cref="Kind"/> is <see cref="IrOperandKind.Register"/>).
    /// </summary>
    public int Register { get; init; }

    /// <summary>
    /// Immediate integer value (when <see cref="Kind"/> is <see cref="IrOperandKind.Immediate"/>).
    /// </summary>
    public int Value { get; init; }

    /// <summary>
    /// Label name (when <see cref="Kind"/> is <see cref="IrOperandKind.Label"/>).
    /// </summary>
    public string? Label { get; init; }

    public static IrOperand Reg(int index) => new() { Kind = IrOperandKind.Register, Register = index };

    public static IrOperand Imm(int value) => new() { Kind = IrOperandKind.Immediate, Value = value };

    public static IrOperand Lbl(string name) => new() { Kind = IrOperandKind.Label, Label = name };
}
