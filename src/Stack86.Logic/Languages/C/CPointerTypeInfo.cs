namespace Stack86.Logic.Languages.C;

/// <summary>
/// A pointer type — always 16 bits (near pointer) on the 8086.
/// </summary>
/// <param name="inner">The type being pointed to.</param>
public sealed class CPointerTypeInfo(CTypeInfo inner) : CTypeInfo
{
    /// <summary>Gets the type being pointed to.</summary>
    public CTypeInfo Inner { get; } = inner;

    /// <inheritdoc />
    public override int SizeInBytes => 2;
}
