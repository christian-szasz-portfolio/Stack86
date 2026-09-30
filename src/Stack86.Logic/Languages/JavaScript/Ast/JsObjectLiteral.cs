namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// An object literal: <c>{ x: 1, y: 2 }</c>.
/// </summary>
public sealed record JsObjectLiteral : JsExpressionNode
{
    /// <summary>The property definitions.</summary>
    public required IReadOnlyList<JsPropertyNode> Properties { get; init; }
}
