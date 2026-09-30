namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A boolean literal (<c>true</c> or <c>false</c>).
/// </summary>
public sealed record CppBoolLiteral : CppExpressionNode
{
    /// <summary>The boolean value.</summary>
    public required bool Value { get; init; }
}
