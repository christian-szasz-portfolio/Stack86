namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A C++ namespace declaration containing inner declarations.
/// </summary>
public sealed record CppNamespaceDeclaration : CppDeclarationNode
{
    /// <summary>Namespace name.</summary>
    public required string Name { get; init; }

    /// <summary>Declarations within this namespace.</summary>
    public required IReadOnlyList<CppDeclarationNode> Declarations { get; init; }
}
