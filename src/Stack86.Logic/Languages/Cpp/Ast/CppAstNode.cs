namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// Base type for all C++ AST nodes.
/// </summary>
public abstract record CppAstNode
{
    /// <summary>Source line where this node begins.</summary>
    public int Line { get; init; }

    /// <summary>Source column where this node begins.</summary>
    public int Column { get; init; }
}
