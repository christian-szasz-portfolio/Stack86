namespace Stack86.Logic.Languages.C;

/// <summary>
/// Classifies a <see cref="CPrimitiveTypeInfo"/>.
/// </summary>
public enum CPrimitiveKind
{
    /// <summary>16-bit signed integer.</summary>
    Int,

    /// <summary>8-bit character.</summary>
    Char,

    /// <summary>Zero-sized void.</summary>
    Void,
}
