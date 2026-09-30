namespace Stack86.Logic.Test.Languages.C;

using System.Linq;
using Stack86.Common.Exceptions;
using Stack86.Logic.Languages.C;

/// <summary>
/// Direct unit tests for <see cref="CLexer"/> covering operators, literals,
/// escape sequences, comments, keywords and error paths.
/// </summary>
[TestClass]
public sealed class CLexerTests
{
    [TestMethod]
    public void Tokenize_EmptySource_ReturnsOnlyEof()
    {
        var tokens = new CLexer(string.Empty).Tokenize();
        Assert.HasCount(1, tokens);
        Assert.AreEqual(CTokenKind.EndOfFile, tokens[0].Kind);
    }

    [TestMethod]
    public void Tokenize_HexLiteral_ParsesValue()
    {
        var t = First("0xFF", CTokenKind.IntegerLiteral);
        Assert.AreEqual(255, t.IntValue);
    }

    [TestMethod]
    public void Tokenize_HexLiteralUppercase_ParsesValue()
    {
        var t = First("0X1A", CTokenKind.IntegerLiteral);
        Assert.AreEqual(26, t.IntValue);
    }

    [TestMethod]
    public void Tokenize_DecimalLiteral_ParsesValue()
    {
        var t = First("123", CTokenKind.IntegerLiteral);
        Assert.AreEqual(123, t.IntValue);
    }

    [TestMethod]
    public void Tokenize_CharLiteral_Plain()
    {
        var t = First("'A'", CTokenKind.CharLiteral);
        Assert.AreEqual('A', t.CharValue);
    }

    [TestMethod]
    public void Tokenize_CharLiteral_Escapes()
    {
        Assert.AreEqual('\n', First("'\\n'", CTokenKind.CharLiteral).CharValue);
        Assert.AreEqual('\t', First("'\\t'", CTokenKind.CharLiteral).CharValue);
        Assert.AreEqual('\r', First("'\\r'", CTokenKind.CharLiteral).CharValue);
        Assert.AreEqual('\0', First("'\\0'", CTokenKind.CharLiteral).CharValue);
        Assert.AreEqual('\\', First("'\\\\'", CTokenKind.CharLiteral).CharValue);
        Assert.AreEqual('\'', First("'\\''", CTokenKind.CharLiteral).CharValue);
        Assert.AreEqual('"', First("'\\\"'", CTokenKind.CharLiteral).CharValue);
    }

    [TestMethod]
    public void Tokenize_CharLiteral_UnknownEscape_Throws()
    {
        Assert.ThrowsExactly<UnsupportedSyntaxException>(() => new CLexer("'\\q'").Tokenize());
    }

    [TestMethod]
    public void Tokenize_CharLiteral_Unterminated_Throws()
    {
        Assert.ThrowsExactly<UnsupportedSyntaxException>(() => new CLexer("'a").Tokenize());
    }

    [TestMethod]
    public void Tokenize_StringLiteral_Plain()
    {
        var t = First("\"hello\"", CTokenKind.StringLiteral);
        Assert.AreEqual("hello", t.StringValue);
    }

    [TestMethod]
    public void Tokenize_StringLiteral_Escapes()
    {
        var t = First("\"a\\n\\tb\"", CTokenKind.StringLiteral);
        Assert.AreEqual("a\n\tb", t.StringValue);
    }

    [TestMethod]
    public void Tokenize_StringLiteral_Unterminated_Throws()
    {
        Assert.ThrowsExactly<UnsupportedSyntaxException>(() => new CLexer("\"oops").Tokenize());
    }

    [TestMethod]
    public void Tokenize_LineComment_IsSkipped()
    {
        var tokens = new CLexer("// comment\nx").Tokenize();
        Assert.IsTrue(tokens.Any(x => x.Kind == CTokenKind.Identifier && x.Text == "x"));
    }

    [TestMethod]
    public void Tokenize_BlockComment_IsSkipped()
    {
        var tokens = new CLexer("/* a\nb */ y").Tokenize();
        Assert.IsTrue(tokens.Any(x => x.Kind == CTokenKind.Identifier && x.Text == "y"));
    }

    [TestMethod]
    public void Tokenize_UnexpectedChar_Throws()
    {
        Assert.ThrowsExactly<UnsupportedSyntaxException>(() => new CLexer("@").Tokenize());
    }

    [TestMethod]
    [DataRow("int", CTokenKind.IntKeyword)]
    [DataRow("char", CTokenKind.CharKeyword)]
    [DataRow("void", CTokenKind.VoidKeyword)]
    [DataRow("return", CTokenKind.ReturnKeyword)]
    [DataRow("if", CTokenKind.IfKeyword)]
    [DataRow("else", CTokenKind.ElseKeyword)]
    [DataRow("while", CTokenKind.WhileKeyword)]
    [DataRow("for", CTokenKind.ForKeyword)]
    [DataRow("break", CTokenKind.BreakKeyword)]
    [DataRow("continue", CTokenKind.ContinueKeyword)]
    [DataRow("struct", CTokenKind.StructKeyword)]
    [DataRow("enum", CTokenKind.EnumKeyword)]
    [DataRow("union", CTokenKind.UnionKeyword)]
    [DataRow("typedef", CTokenKind.TypedefKeyword)]
    [DataRow("do", CTokenKind.DoKeyword)]
    [DataRow("switch", CTokenKind.SwitchKeyword)]
    [DataRow("case", CTokenKind.CaseKeyword)]
    [DataRow("default", CTokenKind.DefaultKeyword)]
    [DataRow("sizeof", CTokenKind.SizeofKeyword)]
    public void Tokenize_Keywords_AreRecognised(string source, CTokenKind expected)
    {
        var t = First(source, expected);
        Assert.AreEqual(source, t.Text);
    }

    [TestMethod]
    [DataRow("<<=", CTokenKind.ShiftLeftEqual)]
    [DataRow(">>=", CTokenKind.ShiftRightEqual)]
    [DataRow("==", CTokenKind.Equal)]
    [DataRow("!=", CTokenKind.NotEqual)]
    [DataRow("<=", CTokenKind.LessEqual)]
    [DataRow(">=", CTokenKind.GreaterEqual)]
    [DataRow("<<", CTokenKind.ShiftLeft)]
    [DataRow(">>", CTokenKind.ShiftRight)]
    [DataRow("&&", CTokenKind.LogicalAnd)]
    [DataRow("||", CTokenKind.LogicalOr)]
    [DataRow("->", CTokenKind.Arrow)]
    [DataRow("++", CTokenKind.PlusPlus)]
    [DataRow("--", CTokenKind.MinusMinus)]
    [DataRow("+=", CTokenKind.PlusEqual)]
    [DataRow("-=", CTokenKind.MinusEqual)]
    [DataRow("*=", CTokenKind.StarEqual)]
    [DataRow("/=", CTokenKind.SlashEqual)]
    [DataRow("%=", CTokenKind.PercentEqual)]
    [DataRow("&=", CTokenKind.AmpEqual)]
    [DataRow("|=", CTokenKind.PipeEqual)]
    [DataRow("^=", CTokenKind.CaretEqual)]
    [DataRow("(", CTokenKind.LeftParen)]
    [DataRow(")", CTokenKind.RightParen)]
    [DataRow("{", CTokenKind.LeftBrace)]
    [DataRow("}", CTokenKind.RightBrace)]
    [DataRow("[", CTokenKind.LeftBracket)]
    [DataRow("]", CTokenKind.RightBracket)]
    [DataRow(";", CTokenKind.Semicolon)]
    [DataRow(",", CTokenKind.Comma)]
    [DataRow(".", CTokenKind.Dot)]
    [DataRow("+", CTokenKind.Plus)]
    [DataRow("-", CTokenKind.Minus)]
    [DataRow("*", CTokenKind.Asterisk)]
    [DataRow("/", CTokenKind.Slash)]
    [DataRow("%", CTokenKind.Percent)]
    [DataRow("&", CTokenKind.Ampersand)]
    [DataRow("|", CTokenKind.Pipe)]
    [DataRow("^", CTokenKind.Caret)]
    [DataRow("~", CTokenKind.Tilde)]
    [DataRow("!", CTokenKind.Exclamation)]
    [DataRow("<", CTokenKind.Less)]
    [DataRow(">", CTokenKind.Greater)]
    [DataRow("=", CTokenKind.Assign)]
    [DataRow("?", CTokenKind.Question)]
    [DataRow(":", CTokenKind.Colon)]
    public void Tokenize_Operators_AreRecognised(string source, CTokenKind expected)
    {
        var t = First(source + " ", expected);
        Assert.AreEqual(expected, t.Kind);
    }

    private static CToken First(string source, CTokenKind kind) =>
        new CLexer(source).Tokenize().First(x => x.Kind == kind);
}
