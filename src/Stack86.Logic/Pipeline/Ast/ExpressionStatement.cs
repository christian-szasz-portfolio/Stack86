namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A statement that wraps an expression.
/// </summary>
public sealed record ExpressionStatement : StatementNode
{
    public required ExpressionNode Expression { get; init; }
}
