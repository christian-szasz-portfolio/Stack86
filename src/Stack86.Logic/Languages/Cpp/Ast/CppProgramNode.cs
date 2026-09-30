namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// Root node of a C++ program.
/// </summary>
public sealed record CppProgramNode : CppAstNode
{
    /// <summary>Top-level declarations (classes, functions, namespaces, using directives, globals).</summary>
    public required IReadOnlyList<CppDeclarationNode> Declarations { get; init; }
}
