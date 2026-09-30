namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A parameter declaration in a function signature.
/// </summary>
public sealed record ParameterDeclaration : AstNode
{
    public required TypeNode Type { get; init; }

    public required string Name { get; init; }
}
