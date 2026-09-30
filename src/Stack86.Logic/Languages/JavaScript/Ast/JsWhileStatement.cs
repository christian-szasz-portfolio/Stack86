namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A while statement: <c>while (cond) { ... }</c>.
/// </summary>
public sealed record JsWhileStatement : JsStatementNode
{
    /// <summary>The loop condition.</summary>
    public required JsExpressionNode Condition { get; init; }

    /// <summary>The loop body.</summary>
    public required JsStatementNode Body { get; init; }
}
