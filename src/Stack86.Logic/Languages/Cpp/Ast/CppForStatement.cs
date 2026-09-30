namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A for loop.
/// </summary>
public sealed record CppForStatement : CppStatementNode
{
    /// <summary>Optional initialiser (expression or variable declaration).</summary>
    public CppStatementNode? Init { get; init; }

    /// <summary>Optional loop condition.</summary>
    public CppExpressionNode? Condition { get; init; }

    /// <summary>Optional post-iteration expression.</summary>
    public CppExpressionNode? Increment { get; init; }

    /// <summary>Loop body.</summary>
    public required CppStatementNode Body { get; init; }
}
