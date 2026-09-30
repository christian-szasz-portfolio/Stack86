namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// Base type for all AST nodes.
/// </summary>
public abstract record AstNode
{
    /// <summary>Source line where this node begins.</summary>
    public int Line { get; init; }

    /// <summary>Source column where this node begins.</summary>
    public int Column { get; init; }
}
