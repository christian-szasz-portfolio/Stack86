namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A string literal expression.
/// </summary>
public sealed record StringLiteral : ExpressionNode
{
    public required string Value { get; init; }
}
