namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// An if/else statement.
/// </summary>
public sealed record CppIfStatement : CppStatementNode
{
    /// <summary>Condition expression.</summary>
    public required CppExpressionNode Condition { get; init; }

    /// <summary>Then branch.</summary>
    public required CppStatementNode Then { get; init; }

    /// <summary>Optional else branch.</summary>
    public CppStatementNode? Else { get; init; }
}
