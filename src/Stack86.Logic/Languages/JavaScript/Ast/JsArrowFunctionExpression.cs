namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// An arrow function expression: <c>(x, y) =&gt; x + y</c> or <c>(x) =&gt; { return x; }</c>.
/// </summary>
public sealed record JsArrowFunctionExpression : JsExpressionNode
{
    /// <summary>The parameter names.</summary>
    public required IReadOnlyList<string> Parameters { get; init; }

    /// <summary>
    /// The function body — either a single expression (concise body)
    /// or a block statement (verbose body). Exactly one is non-null.
    /// </summary>
    public JsExpressionNode? Expression { get; init; }

    /// <summary>The block body (when using braces).</summary>
    public JsBlockStatement? Body { get; init; }
}
