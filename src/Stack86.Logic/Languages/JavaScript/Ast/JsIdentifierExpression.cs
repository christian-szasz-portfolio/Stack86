namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// An identifier reference: <c>x</c>, <c>console</c>, <c>myFunc</c>.
/// </summary>
public sealed record JsIdentifierExpression : JsExpressionNode
{
    /// <summary>The identifier name.</summary>
    public required string Name { get; init; }
}
