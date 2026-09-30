namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// The kind of an IR operand.
/// </summary>
public enum IrOperandKind
{
    /// <summary>A virtual register (temporary).</summary>
    Register,

    /// <summary>An integer literal.</summary>
    Immediate,

    /// <summary>A named label target.</summary>
    Label,
}
