namespace Stack86.Logic.Languages.C;

/// <summary>
/// Metadata for a standard library function recognized by the compiler.
/// </summary>
/// <param name="Name">The canonical function name (e.g. "strlen").</param>
/// <param name="Header">The header that declares this function (e.g. "string.h").</param>
/// <param name="ParameterCount">Expected number of parameters (-1 for variadic).</param>
/// <param name="Kind">Whether the function is expanded inline or dispatched via INT 86h.</param>
/// <param name="ServiceNumber">The INT 86h AH service number (only meaningful when <paramref name="Kind"/> is <see cref="LibraryFunctionKind.Syscall"/>).</param>
public sealed record CLibraryFunction(
    string Name,
    string Header,
    int ParameterCount,
    LibraryFunctionKind Kind,
    int ServiceNumber);
