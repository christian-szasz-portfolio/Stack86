namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A function call expression: <c>callee(arg1, arg2)</c>.
/// </summary>
public sealed record JsCallExpression : JsExpressionNode
{
    /// <summary>The expression being called (identifier or member expression).</summary>
    public required JsExpressionNode Callee { get; init; }

    /// <summary>The list of argument expressions.</summary>
    public required IReadOnlyList<JsExpressionNode> Arguments { get; init; }
}
