namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A character literal expression.
/// </summary>
public sealed record CharLiteral : ExpressionNode
{
    public required char Value { get; init; }
}
