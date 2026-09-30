namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A sizeof expression.
/// </summary>
public sealed record CppSizeofExpression : CppExpressionNode
{
    /// <summary>The type or expression operand (stored as type node if type, otherwise the expression text).</summary>
    public CppTypeNode? Type { get; init; }

    /// <summary>Expression operand when sizeof is applied to an expression rather than a type.</summary>
    public CppExpressionNode? Operand { get; init; }
}
