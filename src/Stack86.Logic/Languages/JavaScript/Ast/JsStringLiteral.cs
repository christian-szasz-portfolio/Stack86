namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A string literal: <c>"hello"</c>, <c>'world'</c>.
/// </summary>
public sealed record JsStringLiteral : JsExpressionNode
{
    /// <summary>The unescaped string value.</summary>
    public required string Value { get; init; }
}
