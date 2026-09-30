namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A function declaration: <c>function name(params) { body }</c>.
/// </summary>
public sealed record JsFunctionDeclaration : JsStatementNode
{
    /// <summary>The function name.</summary>
    public required string Name { get; init; }

    /// <summary>The parameter names.</summary>
    public required IReadOnlyList<string> Parameters { get; init; }

    /// <summary>The function body.</summary>
    public required JsBlockStatement Body { get; init; }
}
