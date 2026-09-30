namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A postfix expression (e.g. <c>i++</c>, <c>i--</c>).
/// </summary>
public sealed record CppPostfixExpression : CppExpressionNode
{
    /// <summary>The operand.</summary>
    public required CppExpressionNode Operand { get; init; }

    /// <summary>The postfix operator.</summary>
    public required CppPostfixOperator Operator { get; init; }
}
