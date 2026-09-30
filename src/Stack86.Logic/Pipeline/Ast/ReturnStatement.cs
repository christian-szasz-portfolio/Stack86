namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A return statement with an optional expression.
/// </summary>
public sealed record ReturnStatement : StatementNode
{
    public ExpressionNode? Expression { get; init; }
}
