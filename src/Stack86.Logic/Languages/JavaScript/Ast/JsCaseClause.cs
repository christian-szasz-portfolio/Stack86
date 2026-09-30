namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A single case clause in a switch statement. <c>Test</c> is null for the default case.
/// </summary>
public sealed record JsCaseClause : JsAstNode
{
    /// <summary>The test expression, or null for the <c>default</c> case.</summary>
    public JsExpressionNode? Test { get; init; }

    /// <summary>The body statements.</summary>
    public required IReadOnlyList<JsStatementNode> Body { get; init; }
}
