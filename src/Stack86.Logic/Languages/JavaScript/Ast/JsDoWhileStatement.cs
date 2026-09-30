namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A do-while statement: <c>do { ... } while (cond);</c>.
/// </summary>
public sealed record JsDoWhileStatement : JsStatementNode
{
    /// <summary>The loop body.</summary>
    public required JsStatementNode Body { get; init; }

    /// <summary>The loop condition.</summary>
    public required JsExpressionNode Condition { get; init; }
}
