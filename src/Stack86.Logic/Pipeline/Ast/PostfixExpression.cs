namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A postfix increment or decrement expression: <c>a++</c> or <c>a--</c>.
/// </summary>
public sealed record PostfixExpression : ExpressionNode
{
    /// <summary>Gets the operator: <see cref="PostfixOperator.Increment"/> or <see cref="PostfixOperator.Decrement"/>.</summary>
    public required PostfixOperator Operator { get; init; }

    /// <summary>Gets the lvalue operand.</summary>
    public required ExpressionNode Operand { get; init; }
}
