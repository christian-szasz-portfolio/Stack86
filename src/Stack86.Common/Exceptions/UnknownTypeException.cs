namespace Stack86.Common.Exceptions;

/// <summary>
/// Thrown when a referenced type (struct, enum, union, typedef, class) cannot be resolved.
/// </summary>
public sealed class UnknownTypeException(string message) : Exception(message);
