namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A do-while loop: <c>do { body } while (condition);</c>.
/// </summary>
public sealed record DoWhileStatement : StatementNode
{
    /// <summary>Gets the loop body.</summary>
    public required StatementNode Body { get; init; }

    /// <summary>Gets the loop condition evaluated after each iteration.</summary>
    public required ExpressionNode Condition { get; init; }
}
