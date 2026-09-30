namespace Stack86.Logic.Languages.C;

/// <summary>
/// Indicates how a standard library function is implemented in the compiler.
/// </summary>
public enum LibraryFunctionKind
{
    /// <summary>
    /// Expanded inline at compile time into existing IR opcodes (e.g. putchar → PrintChar).
    /// </summary>
    Intrinsic,

    /// <summary>
    /// Emitted as an INT 86h software interrupt with a service number, handled by the emulator at runtime.
    /// </summary>
    Syscall,
}
