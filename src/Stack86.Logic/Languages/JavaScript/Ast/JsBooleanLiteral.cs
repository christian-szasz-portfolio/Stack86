namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A boolean literal: <c>true</c> or <c>false</c>.
/// </summary>
public sealed record JsBooleanLiteral : JsExpressionNode
{
    /// <summary>The boolean value.</summary>
    public required bool Value { get; init; }
}
