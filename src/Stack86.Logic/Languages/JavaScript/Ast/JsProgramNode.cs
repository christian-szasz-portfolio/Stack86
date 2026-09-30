namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// Root node of a JavaScript program — a flat list of statements.
/// </summary>
public sealed record JsProgramNode : JsAstNode
{
    /// <summary>Top-level statements (including function declarations).</summary>
    public required IReadOnlyList<JsStatementNode> Statements { get; init; }
}
