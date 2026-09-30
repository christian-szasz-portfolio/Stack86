namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A comma expression: <c>left, right</c> — evaluates both, result is the right side.
/// </summary>
public sealed record CommaExpression : ExpressionNode
{
    /// <summary>Gets the left expression (evaluated for side effects, result discarded).</summary>
    public required ExpressionNode Left { get; init; }

    /// <summary>Gets the right expression (its result is the value of the comma expression).</summary>
    public required ExpressionNode Right { get; init; }
}
