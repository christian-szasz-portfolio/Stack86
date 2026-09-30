namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A ternary conditional expression (e.g. <c>cond ? a : b</c>).
/// </summary>
public sealed record CppTernaryExpression : CppExpressionNode
{
    /// <summary>Condition expression.</summary>
    public required CppExpressionNode Condition { get; init; }

    /// <summary>Expression when condition is true.</summary>
    public required CppExpressionNode TrueExpr { get; init; }

    /// <summary>Expression when condition is false.</summary>
    public required CppExpressionNode FalseExpr { get; init; }
}
