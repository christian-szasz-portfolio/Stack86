namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// The top-level program node containing all declarations.
/// </summary>
public sealed record ProgramNode : AstNode
{
    public required IReadOnlyList<DeclarationNode> Declarations { get; init; }
}
