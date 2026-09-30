namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A static string span inside a template literal.
/// </summary>
public sealed record JsTemplateString : JsTemplatePart
{
    /// <summary>The raw string value of this span.</summary>
    public required string Value { get; init; }
}
