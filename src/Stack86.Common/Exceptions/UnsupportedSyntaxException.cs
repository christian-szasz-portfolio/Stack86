namespace Stack86.Common.Exceptions;

/// <summary>
/// Thrown when a frontend (lexer, parser, IR generator) encounters syntax it does not yet support.
/// </summary>
public sealed class UnsupportedSyntaxException(string message) : Exception(message);
