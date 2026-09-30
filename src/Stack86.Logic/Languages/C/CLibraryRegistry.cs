namespace Stack86.Logic.Languages.C;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Static registry of standard C library functions recognized by the compiler.
/// Maps function names to their metadata including header, parameter count,
/// implementation kind (intrinsic vs. syscall), and INT 86h service number.
/// </summary>
public static class CLibraryRegistry
{
    private static readonly Dictionary<string, CLibraryFunction> Functions = BuildRegistry();

    /// <summary>
    /// Attempts to look up a library function by name.
    /// </summary>
    /// <param name="name">The function name (e.g. "strlen").</param>
    /// <param name="function">The function metadata if found.</param>
    /// <returns><c>true</c> if the function is a known library function.</returns>
    public static bool TryGetFunction(string name, [NotNullWhen(true)] out CLibraryFunction? function)
    {
        return Functions.TryGetValue(name, out function);
    }

    /// <summary>
    /// Checks whether the required header for a library function has been included.
    /// </summary>
    /// <param name="function">The library function to check.</param>
    /// <param name="includedHeaders">The set of system headers that were <c>#include</c>d.</param>
    /// <returns><c>true</c> if the required header is present.</returns>
    public static bool IsHeaderIncluded(CLibraryFunction function, IReadOnlySet<string> includedHeaders)
    {
        return includedHeaders.Contains(function.Header);
    }

    private static Dictionary<string, CLibraryFunction> BuildRegistry()
    {
        var registry = new Dictionary<string, CLibraryFunction>(StringComparer.Ordinal);

        // ── Tier 1: Compiler intrinsics (expanded inline) ──────────────
        Register(registry, "printf", "stdio.h", -1, LibraryFunctionKind.Intrinsic, 0);
        Register(registry, "scanf", "stdio.h", -1, LibraryFunctionKind.Intrinsic, 0);
        Register(registry, "putchar", "stdio.h", 1, LibraryFunctionKind.Intrinsic, 0);
        Register(registry, "puts", "stdio.h", 1, LibraryFunctionKind.Intrinsic, 0);
        Register(registry, "getchar", "stdio.h", 0, LibraryFunctionKind.Intrinsic, 0);
        Register(registry, "abs", "stdlib.h", 1, LibraryFunctionKind.Intrinsic, 0);
        Register(registry, "exit", "stdlib.h", 1, LibraryFunctionKind.Intrinsic, 0);

        // ── Tier 2: INT 86h syscall services ───────────────────────────
        Register(registry, "sleep", "windows.h", 1, LibraryFunctionKind.Syscall, 0x01);
        Register(registry, "Sleep", "windows.h", 1, LibraryFunctionKind.Syscall, 0x01);
        Register(registry, "strlen", "string.h", 1, LibraryFunctionKind.Syscall, 0x02);
        Register(registry, "strcpy", "string.h", 2, LibraryFunctionKind.Syscall, 0x03);
        Register(registry, "strcmp", "string.h", 2, LibraryFunctionKind.Syscall, 0x04);
        Register(registry, "memset", "string.h", 3, LibraryFunctionKind.Syscall, 0x05);
        Register(registry, "rand", "stdlib.h", 0, LibraryFunctionKind.Syscall, 0x06);
        Register(registry, "srand", "stdlib.h", 1, LibraryFunctionKind.Syscall, 0x07);
        Register(registry, "atoi", "stdlib.h", 1, LibraryFunctionKind.Syscall, 0x08);
        Register(registry, "strcat", "string.h", 2, LibraryFunctionKind.Syscall, 0x09);
        Register(registry, "memcpy", "string.h", 3, LibraryFunctionKind.Syscall, 0x0A);
        Register(registry, "toupper", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x0B);
        Register(registry, "tolower", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x0C);

        // ── string.h extended ──────────────────────────────────────────
        Register(registry, "strncpy", "string.h", 3, LibraryFunctionKind.Syscall, 0x0F);
        Register(registry, "strncmp", "string.h", 3, LibraryFunctionKind.Syscall, 0x10);
        Register(registry, "strncat", "string.h", 3, LibraryFunctionKind.Syscall, 0x11);
        Register(registry, "strchr", "string.h", 2, LibraryFunctionKind.Syscall, 0x12);
        Register(registry, "strrchr", "string.h", 2, LibraryFunctionKind.Syscall, 0x13);
        Register(registry, "strstr", "string.h", 2, LibraryFunctionKind.Syscall, 0x14);
        Register(registry, "memcmp", "string.h", 3, LibraryFunctionKind.Syscall, 0x15);

        // ── ctype.h extended ───────────────────────────────────────────
        Register(registry, "isalpha", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x16);
        Register(registry, "isdigit", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x17);
        Register(registry, "isalnum", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x18);
        Register(registry, "isspace", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x19);
        Register(registry, "isupper", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x1A);
        Register(registry, "islower", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x1B);
        Register(registry, "ispunct", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x1C);
        Register(registry, "isprint", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x1D);
        Register(registry, "isxdigit", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x1E);
        Register(registry, "iscntrl", "ctype.h", 1, LibraryFunctionKind.Syscall, 0x1F);

        // ── stdlib.h heap ──────────────────────────────────────────────
        Register(registry, "malloc", "stdlib.h", 1, LibraryFunctionKind.Syscall, 0x20);
        Register(registry, "free", "stdlib.h", 1, LibraryFunctionKind.Syscall, 0x21);
        Register(registry, "calloc", "stdlib.h", 2, LibraryFunctionKind.Syscall, 0x22);
        Register(registry, "realloc", "stdlib.h", 2, LibraryFunctionKind.Syscall, 0x23);

        // ── stdlib.h conversion ────────────────────────────────────────
        Register(registry, "itoa", "stdlib.h", 3, LibraryFunctionKind.Syscall, 0x24);
        Register(registry, "strtol", "stdlib.h", 3, LibraryFunctionKind.Syscall, 0x25);

        // ── time.h ─────────────────────────────────────────────────────
        Register(registry, "time", "time.h", 1, LibraryFunctionKind.Syscall, 0x26);
        Register(registry, "clock", "time.h", 0, LibraryFunctionKind.Syscall, 0x27);
        Register(registry, "difftime", "time.h", 2, LibraryFunctionKind.Syscall, 0x28);

        // ── stdio.h input (scanf support) ──────────────────────────────
        Register(registry, "__readint", "stdio.h", 0, LibraryFunctionKind.Syscall, 0x29);
        Register(registry, "__readstr", "stdio.h", 1, LibraryFunctionKind.Syscall, 0x2A);

        return registry;
    }

    private static void Register(
        Dictionary<string, CLibraryFunction> registry,
        string name,
        string header,
        int parameterCount,
        LibraryFunctionKind kind,
        int serviceNumber)
    {
        registry[name] = new CLibraryFunction(name, header, parameterCount, kind, serviceNumber);
    }
}
