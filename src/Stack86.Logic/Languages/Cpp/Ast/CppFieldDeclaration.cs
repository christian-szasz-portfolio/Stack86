namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A class field (data member) declaration.
/// </summary>
public sealed record CppFieldDeclaration : CppDeclarationNode
{
    /// <summary>Field type.</summary>
    public required CppTypeNode Type { get; init; }

    /// <summary>Field name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional in-class initialiser expression.</summary>
    public CppExpressionNode? Initializer { get; init; }
}
