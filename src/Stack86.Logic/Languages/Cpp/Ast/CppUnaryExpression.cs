namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A unary expression (e.g. <c>-x</c>, <c>!flag</c>, <c>*ptr</c>, <c>&amp;val</c>).
/// </summary>
public sealed record CppUnaryExpression : CppExpressionNode
{
    /// <summary>The unary operator.</summary>
    public required CppUnaryOperator Operator { get; init; }

    /// <summary>The operand.</summary>
    public required CppExpressionNode Operand { get; init; }
}
