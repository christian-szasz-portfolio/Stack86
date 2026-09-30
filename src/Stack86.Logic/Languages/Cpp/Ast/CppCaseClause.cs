namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A case or default clause inside a switch statement.
/// </summary>
public sealed record CppCaseClause : CppAstNode
{
    /// <summary>Case value expression, or <c>null</c> for the default clause.</summary>
    public CppExpressionNode? Value { get; init; }

    /// <summary>Statements under this case label.</summary>
    public required IReadOnlyList<CppStatementNode> Body { get; init; }
}
