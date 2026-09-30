namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A unary expression: <c>OP operand</c> (e.g. <c>-x</c>, <c>!x</c>, <c>typeof x</c>).
/// </summary>
public sealed record JsUnaryExpression : JsExpressionNode
{
    /// <summary>The unary operator.</summary>
    public required JsUnaryOperator Operator { get; init; }

    /// <summary>The operand expression.</summary>
    public required JsExpressionNode Operand { get; init; }
}
