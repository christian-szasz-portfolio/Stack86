namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A switch statement: <c>switch (expr) { case 1: ...; default: ...; }</c>.
/// </summary>
public sealed record SwitchStatement : StatementNode
{
    /// <summary>Gets the expression being switched on.</summary>
    public required ExpressionNode Expression { get; init; }

    /// <summary>Gets the ordered list of case clauses (including default).</summary>
    public required IReadOnlyList<CaseClause> Cases { get; init; }
}
