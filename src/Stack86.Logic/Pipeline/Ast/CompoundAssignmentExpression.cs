namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A compound assignment expression: <c>a += b</c>, <c>a -= b</c>, etc.
/// </summary>
public sealed record CompoundAssignmentExpression : ExpressionNode
{
    /// <summary>Gets the underlying binary operator (Add for +=, Sub for -=, etc.).</summary>
    public required BinaryOperator Operator { get; init; }

    /// <summary>Gets the lvalue target being assigned to.</summary>
    public required ExpressionNode Target { get; init; }

    /// <summary>Gets the right-hand value expression.</summary>
    public required ExpressionNode Value { get; init; }
}
