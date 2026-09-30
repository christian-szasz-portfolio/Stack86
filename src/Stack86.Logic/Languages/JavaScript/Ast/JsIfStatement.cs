namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// An if statement: <c>if (cond) { ... } else { ... }</c>.
/// </summary>
public sealed record JsIfStatement : JsStatementNode
{
    /// <summary>The condition expression.</summary>
    public required JsExpressionNode Condition { get; init; }

    /// <summary>The consequent (then) statement.</summary>
    public required JsStatementNode Consequent { get; init; }

    /// <summary>The optional alternate (else) statement.</summary>
    public JsStatementNode? Alternate { get; init; }
}
