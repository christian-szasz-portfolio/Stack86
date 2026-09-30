namespace Stack86.Logic.Pipeline.X86Conversion;

/// <summary>
/// Capability profile for the Intel 8086 target: 16-bit integers, no floating point,
/// no wide integers, no file I/O.
/// </summary>
public sealed class X8086CapabilityProfile : ITargetCapabilityProfile
{
    /// <inheritdoc />
    public string TargetName => "8086";

    /// <inheritdoc />
    public int MaxIntegerValue => 32767;

    /// <inheritdoc />
    public int MinIntegerValue => -32768;

    /// <inheritdoc />
    public int IntegerSizeBytes => 2;

    /// <inheritdoc />
    public bool SupportsFloatingPoint => false;

    /// <inheritdoc />
    public bool SupportsWideIntegers => false;

    /// <inheritdoc />
    public bool SupportsFileIo => false;

    /// <inheritdoc />
    public IReadOnlySet<string> UnsupportedCTypeKeywords { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "float",
        "double",
        "long",
        "short",
        "signed",
        "unsigned",
        "_Bool",
        "_Complex",
        "_Imaginary",
    };

    /// <inheritdoc />
    public IReadOnlySet<string> UnsupportedCQualifierKeywords { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "const",
        "volatile",
        "register",
        "restrict",
        "_Atomic",
    };
}
