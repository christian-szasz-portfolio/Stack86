namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A character literal.
/// </summary>
public sealed record CppCharLiteral : CppExpressionNode
{
    /// <summary>The character value.</summary>
    public required char Value { get; init; }
}
