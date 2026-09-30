namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// An expression used as a statement.
/// </summary>
public sealed record CppExpressionStatement : CppStatementNode
{
    /// <summary>The expression.</summary>
    public required CppExpressionNode Expression { get; init; }
}
