namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// Unary operators supported by the language.
/// </summary>
public enum UnaryOperator
{
    Negate,
    LogicalNot,
    BitwiseNot,
    Dereference,
    AddressOf,
    PreIncrement,
    PreDecrement,
}
