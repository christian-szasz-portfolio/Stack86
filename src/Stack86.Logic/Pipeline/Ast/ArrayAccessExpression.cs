namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// An array element access expression.
/// </summary>
public sealed record ArrayAccessExpression : ExpressionNode
{
    public required ExpressionNode Array { get; init; }

    public required ExpressionNode Index { get; init; }
}
