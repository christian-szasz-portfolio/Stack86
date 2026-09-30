namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// An integer literal expression.
/// </summary>
public sealed record IntegerLiteral : ExpressionNode
{
    public required int Value { get; init; }
}
