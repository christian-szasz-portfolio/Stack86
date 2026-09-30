namespace Stack86.Logic.Languages.JavaScript;

/// <summary>
/// Inferred value type used during IR generation to select the correct print opcode.
/// </summary>
public enum JsInferredType
{
    /// <summary>A numeric (or boolean/null/undefined) value — maps to 16-bit int.</summary>
    Number,

    /// <summary>A string value — maps to a global data pointer.</summary>
    String,

    /// <summary>An array allocated on the stack.</summary>
    Array,

    /// <summary>An object allocated on the stack.</summary>
    Object,
}
