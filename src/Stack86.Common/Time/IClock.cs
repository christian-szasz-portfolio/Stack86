namespace Stack86.Common.Time;

/// <summary>
/// Abstraction over system time to enable deterministic testing.
/// </summary>
public interface IClock
{
    /// <summary>Gets the current UTC timestamp.</summary>
    DateTimeOffset UtcNow { get; }
}
