namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A variable declaration with optional initializer.
/// </summary>
public sealed record VariableDeclaration : DeclarationNode
{
    public required TypeNode Type { get; init; }

    public required string Name { get; init; }

    public ExpressionNode? Initializer { get; init; }
}
