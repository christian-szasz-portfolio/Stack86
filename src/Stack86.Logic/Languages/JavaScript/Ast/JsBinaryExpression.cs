namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A binary expression: <c>left OP right</c>.
/// </summary>
public sealed record JsBinaryExpression : JsExpressionNode
{
    /// <summary>The left-hand operand.</summary>
    public required JsExpressionNode Left { get; init; }

    /// <summary>The operator.</summary>
    public required JsBinaryOperator Operator { get; init; }

    /// <summary>The right-hand operand.</summary>
    public required JsExpressionNode Right { get; init; }
}
