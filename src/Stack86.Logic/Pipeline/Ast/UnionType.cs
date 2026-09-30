namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A union type reference: <c>union Name</c> used in variable declarations.
/// </summary>
public sealed record UnionType : TypeNode
{
    /// <summary>Gets the name of the union being referenced.</summary>
    public required string Name { get; init; }
}
