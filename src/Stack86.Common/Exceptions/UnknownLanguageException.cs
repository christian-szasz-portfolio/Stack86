namespace Stack86.Common.Exceptions;

/// <summary>
/// Thrown when an unrecognised or unregistered language identifier is requested.
/// </summary>
public sealed class UnknownLanguageException(string message) : Exception(message);
