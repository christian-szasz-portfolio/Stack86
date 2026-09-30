namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A return statement.
/// </summary>
public sealed record CppReturnStatement : CppStatementNode
{
    /// <summary>Optional return value expression.</summary>
    public CppExpressionNode? Value { get; init; }
}
