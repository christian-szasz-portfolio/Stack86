namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A statement that declares a local variable.
/// </summary>
public sealed record VariableDeclarationStatement : StatementNode
{
    public required VariableDeclaration Declaration { get; init; }
}
