namespace Stack86.Logic.Languages.JavaScript;

/// <summary>
/// Token types produced by the JavaScript lexer.
/// </summary>
public enum JsTokenKind
{
    // Literals
    IntegerLiteral,
    StringLiteral,
    TemplateLiteralStart,
    TemplateLiteralMiddle,
    TemplateLiteralEnd,
    TemplateLiteralFull,

    // Identifiers & Keywords
    Identifier,
    VarKeyword,
    LetKeyword,
    ConstKeyword,
    FunctionKeyword,
    ReturnKeyword,
    IfKeyword,
    ElseKeyword,
    WhileKeyword,
    ForKeyword,
    DoKeyword,
    BreakKeyword,
    ContinueKeyword,
    SwitchKeyword,
    CaseKeyword,
    DefaultKeyword,
    TrueKeyword,
    FalseKeyword,
    NullKeyword,
    UndefinedKeyword,
    TypeofKeyword,
    OfKeyword,
    NewKeyword,

    // Delimiters
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Semicolon,
    Comma,
    Dot,
    Colon,
    Question,
    Arrow,

    // Arithmetic operators
    Plus,
    Minus,
    Asterisk,
    Slash,
    Percent,

    // Bitwise operators
    Ampersand,
    Pipe,
    Caret,
    Tilde,
    ShiftLeft,
    ShiftRight,
    UnsignedShiftRight,

    // Increment / Decrement
    PlusPlus,
    MinusMinus,

    // Compound assignment
    PlusEqual,
    MinusEqual,
    StarEqual,
    SlashEqual,
    PercentEqual,
    AmpEqual,
    PipeEqual,
    CaretEqual,
    ShiftLeftEqual,
    ShiftRightEqual,
    UnsignedShiftRightEqual,

    // Comparison
    Equal,
    NotEqual,
    StrictEqual,
    StrictNotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    // Logical
    LogicalAnd,
    LogicalOr,
    Exclamation,

    // Assignment
    Assign,

    EndOfFile,
}
