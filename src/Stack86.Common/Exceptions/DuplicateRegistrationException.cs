namespace Stack86.Common.Exceptions;

/// <summary>
/// Thrown when a registry receives a duplicate registration for the same key.
/// </summary>
public sealed class DuplicateRegistrationException(string message) : Exception(message);
