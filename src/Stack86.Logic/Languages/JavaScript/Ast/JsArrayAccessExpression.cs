namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// An array access expression: <c>array[index]</c>.
/// </summary>
public sealed record JsArrayAccessExpression : JsExpressionNode
{
    /// <summary>The array expression.</summary>
    public required JsExpressionNode Array { get; init; }

    /// <summary>The index expression.</summary>
    public required JsExpressionNode Index { get; init; }
}
