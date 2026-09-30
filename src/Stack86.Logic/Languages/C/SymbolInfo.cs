namespace Stack86.Logic.Languages.C;

/// <summary>
/// Tracks symbol information for a local variable, parameter, or global.
/// </summary>
/// <param name="Register">The virtual register index.</param>
/// <param name="Type">The resolved type.</param>
/// <param name="IsGlobal">Whether this is a global variable.</param>
/// <param name="IsParameter">Whether this is a function parameter.</param>
public record SymbolInfo(int Register, CTypeInfo Type, bool IsGlobal, bool IsParameter);
