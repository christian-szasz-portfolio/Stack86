namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// An assignment expression: <c>target = value</c> or <c>target += value</c>.
/// </summary>
public sealed record JsAssignmentExpression : JsExpressionNode
{
    /// <summary>The assignment target (identifier, member, or array access).</summary>
    public required JsExpressionNode Target { get; init; }

    /// <summary>The assignment operator.</summary>
    public required JsAssignmentOperator Operator { get; init; }

    /// <summary>The value being assigned.</summary>
    public required JsExpressionNode Value { get; init; }
}
