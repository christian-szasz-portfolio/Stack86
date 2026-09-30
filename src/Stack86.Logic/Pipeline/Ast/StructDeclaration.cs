namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A struct type declaration: <c>struct Name { fields... };</c>.
/// </summary>
public sealed record StructDeclaration : DeclarationNode
{
    /// <summary>Gets the name of the struct.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the fields declared inside the struct body.</summary>
    public required IReadOnlyList<StructFieldDeclaration> Fields { get; init; }
}
