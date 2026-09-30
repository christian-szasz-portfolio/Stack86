namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A single case or default clause within a <see cref="SwitchStatement"/>.
/// </summary>
public sealed record CaseClause : AstNode
{
    /// <summary>
    /// Gets the case value expression; <see langword="null"/> for the <c>default</c> case.
    /// </summary>
    public ExpressionNode? Value { get; init; }

    /// <summary>Gets the statements in this case clause.</summary>
    public required IReadOnlyList<StatementNode> Body { get; init; }
}
