namespace Stack86.Logic.Languages.JavaScript;

/// <summary>
/// Tracks information about a JavaScript symbol during IR generation.
/// </summary>
/// <param name="Register">The IR register allocated for this symbol.</param>
/// <param name="Type">The inferred value type.</param>
/// <param name="IsGlobal">Whether this symbol is a global (cross-function) symbol.</param>
/// <param name="IsConst">Whether the symbol was declared with <c>const</c>.</param>
public sealed record JsSymbolInfo(int Register, JsInferredType Type, bool IsGlobal, bool IsConst)
{
    /// <summary>
    /// Gets or sets the number of elements when <see cref="Type"/> is <see cref="JsInferredType.Array"/>.
    /// </summary>
    public int ArrayLength { get; init; }

    /// <summary>
    /// Gets or sets the number of properties when <see cref="Type"/> is <see cref="JsInferredType.Object"/>.
    /// </summary>
    public int FieldCount { get; init; }
}
