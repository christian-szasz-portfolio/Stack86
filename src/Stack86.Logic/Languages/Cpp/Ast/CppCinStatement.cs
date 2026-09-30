namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A <c>cin &gt;&gt; target &gt;&gt; target</c> statement.
/// </summary>
public sealed record CppCinStatement : CppStatementNode
{
    /// <summary>Ordered list of target expressions to read into.</summary>
    public required IReadOnlyList<CppExpressionNode> Targets { get; init; }
}
