namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A switch statement.
/// </summary>
public sealed record CppSwitchStatement : CppStatementNode
{
    /// <summary>Expression being switched on.</summary>
    public required CppExpressionNode Expression { get; init; }

    /// <summary>Case clauses.</summary>
    public required IReadOnlyList<CppCaseClause> Cases { get; init; }
}
