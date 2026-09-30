namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// An integer literal.
/// </summary>
public sealed record CppIntegerLiteral : CppExpressionNode
{
    /// <summary>The integer value.</summary>
    public required int Value { get; init; }
}
