namespace Stack86.Logic.Languages.C;

/// <summary>
/// Base class for semantic type information used during IR generation.
/// Provides size and layout data that the AST <see cref="Pipeline.Ast.TypeNode"/> does not carry.
/// </summary>
public abstract class CTypeInfo
{
    /// <summary>
    /// Size of this type in bytes on the 8086 target.
    /// </summary>
    public abstract int SizeInBytes { get; }

    /// <summary>
    /// Returns <see langword="true"/> when this type can be loaded into registers (fits in AX or DX:AX).
    /// Structs, unions, and arrays are not scalar.
    /// </summary>
    public virtual bool IsScalar => true;
}
