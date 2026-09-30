namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A while loop statement.
/// </summary>
public sealed record WhileStatement : StatementNode
{
    public required ExpressionNode Condition { get; init; }

    public required StatementNode Body { get; init; }
}
