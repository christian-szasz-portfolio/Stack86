namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// An access-labelled section of class members (e.g. <c>public:</c>).
/// </summary>
public sealed record CppAccessSection : CppAstNode
{
    /// <summary>The access modifier for this section.</summary>
    public required CppAccessModifier Access { get; init; }

    /// <summary>Members declared under this access label.</summary>
    public required IReadOnlyList<CppDeclarationNode> Members { get; init; }
}
