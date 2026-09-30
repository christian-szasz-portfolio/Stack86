namespace Stack86.Common.Exceptions;

/// <summary>
/// Thrown when a service or dependency cannot be resolved or constructed.
/// </summary>
public class DependencyInjectionException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="DependencyInjectionException"/> class.</summary>
    public DependencyInjectionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DependencyInjectionException"/> class with an inner exception.</summary>
    public DependencyInjectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
