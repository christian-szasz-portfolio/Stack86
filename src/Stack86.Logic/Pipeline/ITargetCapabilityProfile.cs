namespace Stack86.Logic.Pipeline;

/// <summary>
/// Describes the capabilities and constraints of the compilation target architecture.
/// Language frontends use this to reject unsupported features before parsing.
/// </summary>
public interface ITargetCapabilityProfile
{
    /// <summary>
    /// Gets the human-readable name of the target architecture (e.g. "8086").
    /// </summary>
    string TargetName { get; }

    /// <summary>
    /// Gets the maximum signed integer value the target can represent.
    /// </summary>
    int MaxIntegerValue { get; }

    /// <summary>
    /// Gets the minimum signed integer value the target can represent.
    /// </summary>
    int MinIntegerValue { get; }

    /// <summary>
    /// Gets the integer size in bytes on this target.
    /// </summary>
    int IntegerSizeBytes { get; }

    /// <summary>
    /// Gets a value indicating whether the target supports floating-point types.
    /// </summary>
    bool SupportsFloatingPoint { get; }

    /// <summary>
    /// Gets a value indicating whether the target supports 32-bit or wider integer types.
    /// </summary>
    bool SupportsWideIntegers { get; }

    /// <summary>
    /// Gets a value indicating whether the target supports file I/O operations.
    /// </summary>
    bool SupportsFileIo { get; }

    /// <summary>
    /// Gets the set of C-family type keywords that are <b>not</b> supported on this target.
    /// Used by source-level capability validators.
    /// </summary>
    IReadOnlySet<string> UnsupportedCTypeKeywords { get; }

    /// <summary>
    /// Gets the set of C-family type qualifier keywords that are <b>not</b> supported on this target.
    /// </summary>
    IReadOnlySet<string> UnsupportedCQualifierKeywords { get; }
}
