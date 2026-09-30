namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A string literal.
/// </summary>
public sealed record CppStringLiteral : CppExpressionNode
{
    /// <summary>The unescaped string value.</summary>
    public required string Value { get; init; }
}
