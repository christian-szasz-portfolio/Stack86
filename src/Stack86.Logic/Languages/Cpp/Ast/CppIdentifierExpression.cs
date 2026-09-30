namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// An identifier expression.
/// </summary>
public sealed record CppIdentifierExpression : CppExpressionNode
{
    /// <summary>The identifier name.</summary>
    public required string Name { get; init; }
}
