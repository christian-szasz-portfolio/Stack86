namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A template literal: <c>`Hello ${name}, you are ${age} years old`</c>.
/// </summary>
public sealed record JsTemplateLiteral : JsExpressionNode
{
    /// <summary>The parts of the template, alternating between string spans and expressions.</summary>
    public required IReadOnlyList<JsTemplatePart> Parts { get; init; }
}
