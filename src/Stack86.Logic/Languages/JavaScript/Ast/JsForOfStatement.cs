namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A for-of statement: <c>for (let x of arr) { ... }</c>.
/// </summary>
public sealed record JsForOfStatement : JsStatementNode
{
    /// <summary>The declaration kind (<c>let</c>, <c>const</c>, <c>var</c>).</summary>
    public required JsVariableKind Kind { get; init; }

    /// <summary>The loop variable name.</summary>
    public required string Variable { get; init; }

    /// <summary>The iterable expression (must be an array).</summary>
    public required JsExpressionNode Iterable { get; init; }

    /// <summary>The loop body.</summary>
    public required JsStatementNode Body { get; init; }
}
