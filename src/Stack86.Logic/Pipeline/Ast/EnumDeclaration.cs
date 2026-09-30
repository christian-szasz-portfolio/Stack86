namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// An enum type declaration: <c>enum Name { A, B = 5, C };</c>.
/// </summary>
public sealed record EnumDeclaration : DeclarationNode
{
    /// <summary>Gets the enum tag name (may be empty for anonymous enums).</summary>
    public required string Name { get; init; }

    /// <summary>Gets the ordered list of enum members.</summary>
    public required IReadOnlyList<EnumMemberNode> Members { get; init; }
}
