namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// Unary operators supported by the C++ frontend.
/// </summary>
public enum CppUnaryOperator
{
    Negate,
    LogicalNot,
    BitwiseNot,
    Dereference,
    AddressOf,
    PreIncrement,
    PreDecrement,
}
