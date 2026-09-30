namespace Stack86.Logic.Languages.C;

/// <summary>
/// A primitive C type: <c>int</c>, <c>char</c>, or <c>void</c>.
/// </summary>
public sealed class CPrimitiveTypeInfo : CTypeInfo
{
    /// <summary>Singleton for <c>int</c> (16-bit word).</summary>
    public static readonly CPrimitiveTypeInfo Int = new(CPrimitiveKind.Int, 2);

    /// <summary>Singleton for <c>char</c> (8-bit byte).</summary>
    public static readonly CPrimitiveTypeInfo Char = new(CPrimitiveKind.Char, 1);

    /// <summary>Singleton for <c>void</c> (zero-sized).</summary>
    public static readonly CPrimitiveTypeInfo Void = new(CPrimitiveKind.Void, 0);

    private CPrimitiveTypeInfo(CPrimitiveKind kind, int size)
    {
        this.Kind = kind;
        this.SizeInBytes = size;
    }

    /// <summary>Gets the primitive kind.</summary>
    public CPrimitiveKind Kind { get; }

    /// <inheritdoc />
    public override int SizeInBytes { get; }
}
