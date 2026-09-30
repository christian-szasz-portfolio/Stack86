namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// An expression used as a statement: <c>expr;</c>.
/// </summary>
public sealed record JsExpressionStatement : JsStatementNode
{
    /// <summary>The expression.</summary>
    public required JsExpressionNode Expression { get; init; }
}
