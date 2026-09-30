namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A fixed-size array type: <c>int[10]</c>.
/// </summary>
public sealed record ArrayType : TypeNode
{
    /// <summary>Gets the element type.</summary>
    public required TypeNode ElementType { get; init; }

    /// <summary>Gets the number of elements.</summary>
    public required int Size { get; init; }
}
