namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A function call expression.
/// </summary>
public sealed record CallExpression : ExpressionNode
{
    public required string FunctionName { get; init; }

    public required IReadOnlyList<ExpressionNode> Arguments { get; init; }
}
