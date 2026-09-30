namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// An integer literal: <c>42</c>, <c>0xFF</c>.
/// </summary>
public sealed record JsIntegerLiteral : JsExpressionNode
{
    /// <summary>The integer value.</summary>
    public required int Value { get; init; }
}
