namespace Stack86.Common.Exceptions;

/// <summary>
/// Thrown when an external tool (Node.js transpiler, TCC syntax validator, etc.) fails to execute,
/// times out, exits non-zero, or cannot be located on the host.
/// </summary>
public sealed class ExternalToolException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ExternalToolException"/> class.</summary>
    public ExternalToolException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ExternalToolException"/> class with an inner exception.</summary>
    public ExternalToolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
