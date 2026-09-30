namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// Base type for all JavaScript AST nodes.
/// </summary>
public abstract record JsAstNode
{
    /// <summary>1-based source line where this node begins.</summary>
    public int Line { get; init; }

    /// <summary>1-based source column where this node begins.</summary>
    public int Column { get; init; }
}
