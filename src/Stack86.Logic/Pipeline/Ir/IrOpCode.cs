namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Opcodes for the three-address-code intermediate representation.
/// </summary>
public enum IrOpCode
{
    // Arithmetic
    Add,
    Sub,
    Mul,
    Div,
    Mod,
    Neg,

    // Bitwise
    And,
    Or,
    Xor,
    Not,
    Shl,
    Shr,

    // Comparison (produces 1 or 0)
    CmpEq,
    CmpNe,
    CmpLt,
    CmpLe,
    CmpGt,
    CmpGe,

    // Data movement
    LoadImm,
    Copy,
    LoadMem,
    StoreMem,

    // Control flow
    Label,
    Jump,
    JumpIfZero,
    JumpIfNotZero,
    Call,
    CallIndirect,
    Return,

    // Stack
    Push,
    Pop,

    // Byte-sized memory access
    LoadMem8,
    StoreMem8,

    // Block operations
    BlockCopy,

    // Address computation
    LoadAddress,

    // Load the address of a function (code label) into a register
    LoadFunctionAddress,

    // I/O (printf expansion)
    PrintChar,
    PrintStr,
    PrintInt,

    // Standard library (INT 86h)
    Syscall,

    // Miscellaneous
    Nop,
}
