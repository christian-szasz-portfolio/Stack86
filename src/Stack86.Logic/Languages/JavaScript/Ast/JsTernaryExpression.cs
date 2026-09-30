namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A ternary (conditional) expression: <c>condition ? consequent : alternate</c>.
/// </summary>
public sealed record JsTernaryExpression : JsExpressionNode
{
    /// <summary>The condition.</summary>
    public required JsExpressionNode Condition { get; init; }

    /// <summary>The expression when the condition is truthy.</summary>
    public required JsExpressionNode Consequent { get; init; }

    /// <summary>The expression when the condition is falsy.</summary>
    public required JsExpressionNode Alternate { get; init; }
}
