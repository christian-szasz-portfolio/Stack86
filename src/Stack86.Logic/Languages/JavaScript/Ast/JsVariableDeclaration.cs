namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A variable declaration statement: <c>let x = 42;</c>, <c>const y = "hi";</c>.
/// </summary>
public sealed record JsVariableDeclaration : JsStatementNode
{
    /// <summary>The declaration kind (<c>var</c>, <c>let</c>, <c>const</c>).</summary>
    public required JsVariableKind Kind { get; init; }

    /// <summary>The variable name.</summary>
    public required string Name { get; init; }

    /// <summary>The optional initialiser expression.</summary>
    public JsExpressionNode? Initializer { get; init; }
}
