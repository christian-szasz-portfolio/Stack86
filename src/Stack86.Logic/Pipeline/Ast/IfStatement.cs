namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// An if statement with optional else branch.
/// </summary>
public sealed record IfStatement : StatementNode
{
    public required ExpressionNode Condition { get; init; }

    public required StatementNode Then { get; init; }

    public StatementNode? Else { get; init; }
}
