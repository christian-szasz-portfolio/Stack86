namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A comma expression: <c>expr1, expr2, expr3</c> (value is the last).
/// </summary>
public sealed record JsCommaExpression : JsExpressionNode
{
    /// <summary>The sub-expressions (left to right).</summary>
    public required IReadOnlyList<JsExpressionNode> Expressions { get; init; }
}
