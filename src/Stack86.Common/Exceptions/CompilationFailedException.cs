namespace Stack86.Common.Exceptions;

/// <summary>
/// Thrown when a compilation stage fails for a recoverable, user-facing reason (for example an
/// unsupported language, a transpiler error, or an unprocessable IR program). Unlike
/// <see cref="CompilerInvariantException"/>, this represents an expected failure mode that should
/// be surfaced to the caller rather than treated as a compiler bug.
/// </summary>
public sealed class CompilationFailedException(string message) : Exception(message);
