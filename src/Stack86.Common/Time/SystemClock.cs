namespace Stack86.Common.Time;

/// <summary>
/// Default <see cref="IClock"/> implementation backed by <see cref="DateTimeOffset.UtcNow"/>.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc/>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
