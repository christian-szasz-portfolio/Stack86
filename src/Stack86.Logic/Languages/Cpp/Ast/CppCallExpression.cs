namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A function/method call expression.
/// </summary>
public sealed record CppCallExpression : CppExpressionNode
{
    /// <summary>The callee expression (identifier, scope resolution, or member access).</summary>
    public required CppExpressionNode Callee { get; init; }

    /// <summary>Call arguments.</summary>
    public required IReadOnlyList<CppExpressionNode> Arguments { get; init; }
}
