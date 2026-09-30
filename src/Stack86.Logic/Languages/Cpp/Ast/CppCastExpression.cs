namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A cast expression (C-style cast: <c>(int)x</c>).
/// </summary>
public sealed record CppCastExpression : CppExpressionNode
{
    /// <summary>Target type.</summary>
    public required CppTypeNode Type { get; init; }

    /// <summary>Expression being cast.</summary>
    public required CppExpressionNode Operand { get; init; }
}
