namespace Stack86.Logic.Test.Languages.JavaScript;

using System.Linq;
using Stack86.Common.Exceptions;
using Stack86.Logic.Languages.JavaScript;

/// <summary>
/// Direct unit tests for <see cref="JsLexer"/> covering operators, literals,
/// template strings, escapes, comments and error paths.
/// </summary>
[TestClass]
public sealed class JsLexerTests
{
    [TestMethod]
    public void Tokenize_EmptySource_ReturnsOnlyEof()
    {
        var tokens = new JsLexer(string.Empty).Tokenize();
        Assert.HasCount(1, tokens);
        Assert.AreEqual(JsTokenKind.EndOfFile, tokens[0].Kind);
    }

    [TestMethod]
    public void Tokenize_HexLiteral_ParsesValue()
    {
        var t = First("0xFF", JsTokenKind.IntegerLiteral);
        Assert.AreEqual(255, t.IntValue);
    }

    [TestMethod]
    public void Tokenize_DecimalLiteral_ParsesValue()
    {
        var t = First("42", JsTokenKind.IntegerLiteral);
        Assert.AreEqual(42, t.IntValue);
    }

    [TestMethod]
    public void Tokenize_Identifier_DollarAndUnderscore()
    {
        var tokens = new JsLexer("$x _y a$_b").Tokenize();
        var ids = tokens.Where(x => x.Kind == JsTokenKind.Identifier).Select(x => x.Text).ToList();
        Assert.HasCount(3, ids);
        Assert.AreEqual("$x", ids[0]);
        Assert.AreEqual("_y", ids[1]);
        Assert.AreEqual("a$_b", ids[2]);
    }

    [TestMethod]
    public void Tokenize_StringLiteral_DoubleQuote()
    {
        var t = First("\"hello\"", JsTokenKind.StringLiteral);
        Assert.AreEqual("hello", t.StringValue);
    }

    [TestMethod]
    public void Tokenize_StringLiteral_Escapes()
    {
        var t = First("\"a\\nb\\t\\r\\0\\\\\\\"\\'\"", JsTokenKind.StringLiteral);
        Assert.AreEqual("a\nb\t\r\0\\\"'", t.StringValue);
    }

    [TestMethod]
    public void Tokenize_StringLiteral_UnknownEscapeKept()
    {
        var t = First("\"\\q\"", JsTokenKind.StringLiteral);
        Assert.AreEqual("q", t.StringValue);
    }

    [TestMethod]
    public void Tokenize_StringLiteral_Unterminated_Throws()
    {
        Assert.ThrowsExactly<UnsupportedSyntaxException>(() => new JsLexer("\"oops").Tokenize());
    }

    [TestMethod]
    public void Tokenize_TemplateLiteral_Plain()
    {
        var t = First("`hello`", JsTokenKind.TemplateLiteralFull);
        Assert.AreEqual("hello", t.StringValue);
    }

    [TestMethod]
    public void Tokenize_TemplateLiteral_WithBackslashEscape()
    {
        var t = First("`a\\nb`", JsTokenKind.TemplateLiteralFull);
        Assert.AreEqual("a\nb", t.StringValue);
    }

    [TestMethod]
    public void Tokenize_TemplateLiteral_Unterminated_Throws()
    {
        Assert.ThrowsExactly<UnsupportedSyntaxException>(() => new JsLexer("`oops").Tokenize());
    }

    [TestMethod]
    public void Tokenize_LineComment_IsSkipped()
    {
        var tokens = new JsLexer("// comment\nx").Tokenize();
        Assert.IsTrue(tokens.Any(x => x.Kind == JsTokenKind.Identifier && x.Text == "x"));
    }

    [TestMethod]
    public void Tokenize_BlockComment_IsSkipped()
    {
        var tokens = new JsLexer("/* a\nb */ y").Tokenize();
        Assert.IsTrue(tokens.Any(x => x.Kind == JsTokenKind.Identifier && x.Text == "y"));
    }

    [TestMethod]
    public void Tokenize_UnexpectedChar_Throws()
    {
        Assert.ThrowsExactly<UnsupportedSyntaxException>(() => new JsLexer("@").Tokenize());
    }

    [TestMethod]
    [DataRow("var", JsTokenKind.VarKeyword)]
    [DataRow("let", JsTokenKind.LetKeyword)]
    [DataRow("const", JsTokenKind.ConstKeyword)]
    [DataRow("function", JsTokenKind.FunctionKeyword)]
    [DataRow("return", JsTokenKind.ReturnKeyword)]
    [DataRow("if", JsTokenKind.IfKeyword)]
    [DataRow("else", JsTokenKind.ElseKeyword)]
    [DataRow("while", JsTokenKind.WhileKeyword)]
    [DataRow("for", JsTokenKind.ForKeyword)]
    [DataRow("do", JsTokenKind.DoKeyword)]
    [DataRow("break", JsTokenKind.BreakKeyword)]
    [DataRow("continue", JsTokenKind.ContinueKeyword)]
    [DataRow("switch", JsTokenKind.SwitchKeyword)]
    [DataRow("case", JsTokenKind.CaseKeyword)]
    [DataRow("default", JsTokenKind.DefaultKeyword)]
    [DataRow("true", JsTokenKind.TrueKeyword)]
    [DataRow("false", JsTokenKind.FalseKeyword)]
    [DataRow("null", JsTokenKind.NullKeyword)]
    [DataRow("undefined", JsTokenKind.UndefinedKeyword)]
    [DataRow("typeof", JsTokenKind.TypeofKeyword)]
    [DataRow("of", JsTokenKind.OfKeyword)]
    [DataRow("new", JsTokenKind.NewKeyword)]
    public void Tokenize_Keywords_AreRecognised(string source, JsTokenKind expected)
    {
        var t = First(source, expected);
        Assert.AreEqual(source, t.Text);
    }

    [TestMethod]
    [DataRow("===", JsTokenKind.StrictEqual)]
    [DataRow("!==", JsTokenKind.StrictNotEqual)]
    [DataRow("<<=", JsTokenKind.ShiftLeftEqual)]
    [DataRow(">>=", JsTokenKind.ShiftRightEqual)]
    [DataRow(">>>", JsTokenKind.UnsignedShiftRight)]
    [DataRow(">>>=", JsTokenKind.UnsignedShiftRightEqual)]
    [DataRow("==", JsTokenKind.Equal)]
    [DataRow("!=", JsTokenKind.NotEqual)]
    [DataRow("<=", JsTokenKind.LessEqual)]
    [DataRow(">=", JsTokenKind.GreaterEqual)]
    [DataRow("<<", JsTokenKind.ShiftLeft)]
    [DataRow(">>", JsTokenKind.ShiftRight)]
    [DataRow("&&", JsTokenKind.LogicalAnd)]
    [DataRow("||", JsTokenKind.LogicalOr)]
    [DataRow("=>", JsTokenKind.Arrow)]
    [DataRow("++", JsTokenKind.PlusPlus)]
    [DataRow("--", JsTokenKind.MinusMinus)]
    [DataRow("+=", JsTokenKind.PlusEqual)]
    [DataRow("-=", JsTokenKind.MinusEqual)]
    [DataRow("*=", JsTokenKind.StarEqual)]
    [DataRow("/=", JsTokenKind.SlashEqual)]
    [DataRow("%=", JsTokenKind.PercentEqual)]
    [DataRow("&=", JsTokenKind.AmpEqual)]
    [DataRow("|=", JsTokenKind.PipeEqual)]
    [DataRow("^=", JsTokenKind.CaretEqual)]
    [DataRow("(", JsTokenKind.LeftParen)]
    [DataRow(")", JsTokenKind.RightParen)]
    [DataRow("{", JsTokenKind.LeftBrace)]
    [DataRow("}", JsTokenKind.RightBrace)]
    [DataRow("[", JsTokenKind.LeftBracket)]
    [DataRow("]", JsTokenKind.RightBracket)]
    [DataRow(";", JsTokenKind.Semicolon)]
    [DataRow(",", JsTokenKind.Comma)]
    [DataRow(".", JsTokenKind.Dot)]
    [DataRow(":", JsTokenKind.Colon)]
    [DataRow("?", JsTokenKind.Question)]
    [DataRow("+", JsTokenKind.Plus)]
    [DataRow("-", JsTokenKind.Minus)]
    [DataRow("*", JsTokenKind.Asterisk)]
    [DataRow("/", JsTokenKind.Slash)]
    [DataRow("%", JsTokenKind.Percent)]
    [DataRow("&", JsTokenKind.Ampersand)]
    [DataRow("|", JsTokenKind.Pipe)]
    [DataRow("^", JsTokenKind.Caret)]
    [DataRow("~", JsTokenKind.Tilde)]
    [DataRow("!", JsTokenKind.Exclamation)]
    [DataRow("<", JsTokenKind.Less)]
    [DataRow(">", JsTokenKind.Greater)]
    [DataRow("=", JsTokenKind.Assign)]
    public void Tokenize_Operators_AreRecognised(string source, JsTokenKind expected)
    {
        var t = First(source + " ", expected);
        Assert.AreEqual(expected, t.Kind);
    }

    private static JsToken First(string source, JsTokenKind kind) =>
        new JsLexer(source).Tokenize().First(x => x.Kind == kind);
}
