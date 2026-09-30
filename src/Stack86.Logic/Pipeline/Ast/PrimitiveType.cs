namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A primitive type (int, char, void).
/// </summary>
public sealed record PrimitiveType : TypeNode
{
    public required PrimitiveKind Kind { get; init; }
}
