namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// An interpolated expression inside a template literal: <c>${expr}</c>.
/// </summary>
public sealed record JsTemplateExpression : JsTemplatePart
{
    /// <summary>The embedded expression.</summary>
    public required JsExpressionNode Expression { get; init; }
}
