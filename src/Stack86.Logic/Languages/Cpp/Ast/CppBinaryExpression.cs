namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A binary expression (e.g. <c>a + b</c>, <c>x == y</c>).
/// </summary>
public sealed record CppBinaryExpression : CppExpressionNode
{
    /// <summary>Left operand.</summary>
    public required CppExpressionNode Left { get; init; }

    /// <summary>The binary operator.</summary>
    public required CppBinaryOperator Operator { get; init; }

    /// <summary>Right operand.</summary>
    public required CppExpressionNode Right { get; init; }
}
