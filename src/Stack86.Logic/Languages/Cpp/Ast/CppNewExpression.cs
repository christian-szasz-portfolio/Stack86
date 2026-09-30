namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A <c>new T(args)</c> expression.
/// </summary>
public sealed record CppNewExpression : CppExpressionNode
{
    /// <summary>The type being allocated.</summary>
    public required CppTypeNode Type { get; init; }

    /// <summary>Constructor arguments.</summary>
    public IReadOnlyList<CppExpressionNode> Arguments { get; init; } = [];
}
