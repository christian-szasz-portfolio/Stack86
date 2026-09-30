namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// Assignment operator kinds (includes compound assignment).
/// </summary>
public enum JsAssignmentOperator
{
    Assign,
    AddAssign,
    SubAssign,
    MulAssign,
    DivAssign,
    ModAssign,
    AndAssign,
    OrAssign,
    XorAssign,
    ShlAssign,
    ShrAssign,
    UnsignedShrAssign,
}
