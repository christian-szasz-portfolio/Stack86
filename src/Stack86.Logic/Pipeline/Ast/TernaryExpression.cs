namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A ternary conditional expression: <c>condition ? thenExpr : elseExpr</c>.
/// </summary>
public sealed record TernaryExpression : ExpressionNode
{
    /// <summary>Gets the condition expression.</summary>
    public required ExpressionNode Condition { get; init; }

    /// <summary>Gets the expression evaluated when the condition is true.</summary>
    public required ExpressionNode ThenExpression { get; init; }

    /// <summary>Gets the expression evaluated when the condition is false.</summary>
    public required ExpressionNode ElseExpression { get; init; }
}
