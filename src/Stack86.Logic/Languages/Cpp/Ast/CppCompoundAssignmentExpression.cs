namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A compound assignment expression (e.g. <c>x += 1</c>).
/// </summary>
public sealed record CppCompoundAssignmentExpression : CppExpressionNode
{
    /// <summary>Left-hand side (target).</summary>
    public required CppExpressionNode Left { get; init; }

    /// <summary>The compound operator (Add, Sub, etc.).</summary>
    public required CppBinaryOperator Operator { get; init; }

    /// <summary>Right-hand side (value).</summary>
    public required CppExpressionNode Right { get; init; }
}
