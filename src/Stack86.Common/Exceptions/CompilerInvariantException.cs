namespace Stack86.Common.Exceptions;

/// <summary>
/// Thrown when a compiler invariant is violated (an "unreachable" branch was reached, an unexpected
/// AST/IR node shape was observed). Indicates a compiler bug, not a user error.
/// </summary>
public sealed class CompilerInvariantException(string message) : Exception(message);
