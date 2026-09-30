namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A struct field declaration: a type and a name.
/// </summary>
public sealed record StructFieldDeclaration : AstNode
{
    /// <summary>Gets the type of the field.</summary>
    public required TypeNode Type { get; init; }

    /// <summary>Gets the name of the field.</summary>
    public required string Name { get; init; }
}
