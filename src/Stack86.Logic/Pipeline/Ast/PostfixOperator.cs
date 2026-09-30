namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// Operators for postfix expressions.
/// </summary>
public enum PostfixOperator
{
    /// <summary>Postfix increment: <c>a++</c>.</summary>
    Increment,

    /// <summary>Postfix decrement: <c>a--</c>.</summary>
    Decrement,
}
