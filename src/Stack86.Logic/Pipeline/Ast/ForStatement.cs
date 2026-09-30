namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A for loop statement.
/// </summary>
public sealed record ForStatement : StatementNode
{
    public StatementNode? Init { get; init; }

    public ExpressionNode? Condition { get; init; }

    public ExpressionNode? Increment { get; init; }

    public required StatementNode Body { get; init; }
}
