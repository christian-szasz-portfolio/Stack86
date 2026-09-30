namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A unary expression with an operator and operand.
/// </summary>
public sealed record UnaryExpression : ExpressionNode
{
    public required UnaryOperator Operator { get; init; }

    public required ExpressionNode Operand { get; init; }
}
