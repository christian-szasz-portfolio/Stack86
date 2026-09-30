namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A for statement: <c>for (init; cond; update) { ... }</c>.
/// </summary>
public sealed record JsForStatement : JsStatementNode
{
    /// <summary>The optional initialiser (variable declaration or expression statement).</summary>
    public JsStatementNode? Init { get; init; }

    /// <summary>The optional loop condition.</summary>
    public JsExpressionNode? Condition { get; init; }

    /// <summary>The optional update expression.</summary>
    public JsExpressionNode? Update { get; init; }

    /// <summary>The loop body.</summary>
    public required JsStatementNode Body { get; init; }
}
