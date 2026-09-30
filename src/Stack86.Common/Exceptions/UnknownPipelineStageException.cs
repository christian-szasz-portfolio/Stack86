namespace Stack86.Common.Exceptions;

/// <summary>
/// Thrown when an unrecognised pipeline stage identifier is referenced.
/// </summary>
public sealed class UnknownPipelineStageException(string message) : Exception(message);
