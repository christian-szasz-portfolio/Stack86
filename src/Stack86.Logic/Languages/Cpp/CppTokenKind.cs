namespace Stack86.Logic.Languages.Cpp;

/// <summary>
/// Token types produced by the C++ lexer.
/// Extends C token kinds with C++-specific keywords and operators.
/// </summary>
public enum CppTokenKind
{
    // ── Literals ───────────────────────────────────────────
    IntegerLiteral,
    CharLiteral,
    StringLiteral,

    // ── Identifiers ───────────────────────────────────────
    Identifier,

    // ── C keywords (shared) ───────────────────────────────
    IntKeyword,
    CharKeyword,
    VoidKeyword,
    ReturnKeyword,
    IfKeyword,
    ElseKeyword,
    WhileKeyword,
    ForKeyword,
    BreakKeyword,
    ContinueKeyword,
    StructKeyword,
    EnumKeyword,
    UnionKeyword,
    TypedefKeyword,
    DoKeyword,
    SwitchKeyword,
    CaseKeyword,
    DefaultKeyword,
    SizeofKeyword,
    ConstKeyword,
    StaticKeyword,
    ExternKeyword,
    SignedKeyword,
    UnsignedKeyword,
    LongKeyword,
    ShortKeyword,

    // ── C++ keywords ──────────────────────────────────────
    ClassKeyword,
    PublicKeyword,
    PrivateKeyword,
    ProtectedKeyword,
    NamespaceKeyword,
    UsingKeyword,
    TemplateKeyword,
    TypenameKeyword,
    NewKeyword,
    DeleteKeyword,
    BoolKeyword,
    TrueKeyword,
    FalseKeyword,
    VirtualKeyword,
    OverrideKeyword,
    ThisKeyword,
    NullptrKeyword,
    OperatorKeyword,
    FriendKeyword,
    InlineKeyword,

    // ── Delimiters & punctuation ──────────────────────────
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Semicolon,
    Comma,
    Dot,
    Arrow,
    ScopeResolution,
    Asterisk,
    Ampersand,
    Colon,
    Question,

    // ── Arithmetic & bitwise ──────────────────────────────
    Plus,
    Minus,
    Slash,
    Percent,
    Pipe,
    Caret,
    Tilde,
    Exclamation,
    ShiftLeft,
    ShiftRight,

    // ── Increment/decrement ───────────────────────────────
    PlusPlus,
    MinusMinus,

    // ── Compound assignment ───────────────────────────────
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

    // ── Comparison ────────────────────────────────────────
    Equal,
    NotEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    // ── Logical ───────────────────────────────────────────
    LogicalAnd,
    LogicalOr,

    // ── Assignment ────────────────────────────────────────
    Assign,

    // ── End of file ───────────────────────────────────────
    EndOfFile,
}
