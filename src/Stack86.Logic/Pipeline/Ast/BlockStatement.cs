namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A block statement containing a list of statements.
/// </summary>
public sealed record BlockStatement : StatementNode
{
    public required IReadOnlyList<StatementNode> Statements { get; init; }
}
