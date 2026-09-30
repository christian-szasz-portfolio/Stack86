namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A typedef declaration: <c>typedef OldType NewName;</c>.
/// </summary>
public sealed record TypedefDeclaration : DeclarationNode
{
    /// <summary>Gets the original type being aliased.</summary>
    public required TypeNode OriginalType { get; init; }

    /// <summary>Gets the alias name.</summary>
    public required string AliasName { get; init; }
}
