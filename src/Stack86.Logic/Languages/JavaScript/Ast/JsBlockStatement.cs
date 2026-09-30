namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A block statement: <c>{ stmt1; stmt2; }</c>.
/// </summary>
public sealed record JsBlockStatement : JsStatementNode
{
    /// <summary>The statements in the block.</summary>
    public required IReadOnlyList<JsStatementNode> Statements { get; init; }
}
