namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A variable declaration used as a statement inside a block.
/// </summary>
public sealed record CppVariableDeclarationStatement : CppStatementNode
{
    /// <summary>The variable declaration.</summary>
    public required CppVariableDeclaration Declaration { get; init; }
}
