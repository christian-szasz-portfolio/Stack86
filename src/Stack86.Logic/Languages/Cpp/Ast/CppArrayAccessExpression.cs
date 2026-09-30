namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// An array subscript expression (e.g. <c>arr[i]</c>).
/// </summary>
public sealed record CppArrayAccessExpression : CppExpressionNode
{
    /// <summary>The array expression.</summary>
    public required CppExpressionNode Array { get; init; }

    /// <summary>The index expression.</summary>
    public required CppExpressionNode Index { get; init; }
}
