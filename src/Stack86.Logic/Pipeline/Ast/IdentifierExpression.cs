namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// An identifier expression referencing a named variable or parameter.
/// </summary>
public sealed record IdentifierExpression : ExpressionNode
{
    public required string Name { get; init; }
}
