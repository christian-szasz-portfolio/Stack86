namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// An array literal: <c>[1, 2, 3]</c>.
/// </summary>
public sealed record JsArrayLiteral : JsExpressionNode
{
    /// <summary>The element expressions.</summary>
    public required IReadOnlyList<JsExpressionNode> Elements { get; init; }
}
