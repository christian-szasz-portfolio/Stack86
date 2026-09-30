namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A pointer type that wraps an inner type.
/// </summary>
public sealed record PointerType : TypeNode
{
    public required TypeNode Inner { get; init; }
}
