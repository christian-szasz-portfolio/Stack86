namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A type cast expression.
/// </summary>
public sealed record CastExpression : ExpressionNode
{
    public required TypeNode TargetType { get; init; }

    public required ExpressionNode Operand { get; init; }
}
