namespace Stack86.Logic.Languages.JavaScript;

/// <summary>
/// The kind of a JavaScript variable declaration.
/// </summary>
public enum JsVariableKind
{
    /// <summary>Function-scoped, hoisted.</summary>
    Var,

    /// <summary>Block-scoped.</summary>
    Let,

    /// <summary>Block-scoped, immutable binding.</summary>
    Const,
}
