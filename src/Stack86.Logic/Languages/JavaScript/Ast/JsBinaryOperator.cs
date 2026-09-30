namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// Binary operator kinds for JavaScript binary expressions.
/// </summary>
public enum JsBinaryOperator
{
    Add,
    Sub,
    Mul,
    Div,
    Mod,
    BitwiseAnd,
    BitwiseOr,
    BitwiseXor,
    Shl,
    Shr,
    UnsignedShr,
    Equal,
    NotEqual,
    StrictEqual,
    StrictNotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,
    LogicalAnd,
    LogicalOr,
}
