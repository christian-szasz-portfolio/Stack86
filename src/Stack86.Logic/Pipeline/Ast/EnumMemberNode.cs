namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A single member in an <see cref="EnumDeclaration"/>.
/// </summary>
public sealed record EnumMemberNode : AstNode
{
    /// <summary>Gets the member name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional explicit integer value.</summary>
    public ExpressionNode? Value { get; init; }
}
