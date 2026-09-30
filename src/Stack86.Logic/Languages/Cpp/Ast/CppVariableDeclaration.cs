namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A global or local variable declaration.
/// </summary>
public sealed record CppVariableDeclaration : CppDeclarationNode
{
    /// <summary>Variable type.</summary>
    public required CppTypeNode Type { get; init; }

    /// <summary>Variable name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional initialiser expression.</summary>
    public CppExpressionNode? Initializer { get; init; }
}
