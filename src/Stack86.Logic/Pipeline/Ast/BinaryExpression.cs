namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A binary expression with left operand, operator, and right operand.
/// </summary>
public sealed record BinaryExpression : ExpressionNode
{
    public required BinaryOperator Operator { get; init; }

    public required ExpressionNode Left { get; init; }

    public required ExpressionNode Right { get; init; }
}
