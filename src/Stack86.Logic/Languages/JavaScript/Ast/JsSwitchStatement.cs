namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A switch statement: <c>switch (expr) { case v: ... default: ... }</c>.
/// </summary>
public sealed record JsSwitchStatement : JsStatementNode
{
    /// <summary>The discriminant expression.</summary>
    public required JsExpressionNode Discriminant { get; init; }

    /// <summary>The case clauses (including default).</summary>
    public required IReadOnlyList<JsCaseClause> Cases { get; init; }
}
