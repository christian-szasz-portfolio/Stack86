namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A while loop.
/// </summary>
public sealed record CppWhileStatement : CppStatementNode
{
    /// <summary>Loop condition.</summary>
    public required CppExpressionNode Condition { get; init; }

    /// <summary>Loop body.</summary>
    public required CppStatementNode Body { get; init; }
}
