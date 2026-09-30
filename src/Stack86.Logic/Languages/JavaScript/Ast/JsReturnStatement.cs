namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A return statement: <c>return expr;</c>.
/// </summary>
public sealed record JsReturnStatement : JsStatementNode
{
    /// <summary>The optional return value.</summary>
    public JsExpressionNode? Value { get; init; }
}
