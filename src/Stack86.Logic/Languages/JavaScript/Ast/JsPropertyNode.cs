namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A single property in an object literal.
/// </summary>
public sealed record JsPropertyNode : JsAstNode
{
    /// <summary>The property name.</summary>
    public required string Key { get; init; }

    /// <summary>The property value expression.</summary>
    public required JsExpressionNode Value { get; init; }
}
