namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A member access expression: <c>object.property</c>.
/// </summary>
public sealed record JsMemberExpression : JsExpressionNode
{
    /// <summary>The object being accessed.</summary>
    public required JsExpressionNode Object { get; init; }

    /// <summary>The property name.</summary>
    public required string Property { get; init; }
}
