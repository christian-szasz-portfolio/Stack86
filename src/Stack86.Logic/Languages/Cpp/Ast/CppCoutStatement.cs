namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A <c>cout &lt;&lt; expr &lt;&lt; expr</c> statement.
/// </summary>
public sealed record CppCoutStatement : CppStatementNode
{
    /// <summary>Ordered list of expressions inserted into the stream.</summary>
    public required IReadOnlyList<CppExpressionNode> Expressions { get; init; }
}
