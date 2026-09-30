namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A <c>delete expr</c> expression.
/// </summary>
public sealed record CppDeleteExpression : CppExpressionNode
{
    /// <summary>The expression being deleted.</summary>
    public required CppExpressionNode Operand { get; init; }
}
