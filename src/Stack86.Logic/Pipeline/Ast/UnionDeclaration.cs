namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A union type declaration: <c>union Name { int i; char c; };</c>.
/// </summary>
public sealed record UnionDeclaration : DeclarationNode
{
    /// <summary>Gets the union tag name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the ordered list of fields.</summary>
    public required IReadOnlyList<StructFieldDeclaration> Fields { get; init; }
}
