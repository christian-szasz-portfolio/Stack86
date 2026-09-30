namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// A single part of a template literal — either a string span or an embedded expression.
/// </summary>
public abstract record JsTemplatePart : JsAstNode;
