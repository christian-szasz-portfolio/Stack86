namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A do-while loop.
/// </summary>
public sealed record CppDoWhileStatement : CppStatementNode
{
    /// <summary>Loop body.</summary>
    public required CppStatementNode Body { get; init; }

    /// <summary>Loop condition.</summary>
    public required CppExpressionNode Condition { get; init; }
}
