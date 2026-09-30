namespace Stack86.Logic.Test.Languages.Cpp;

using System.Linq;
using Stack86.Common.Exceptions;
using Stack86.Logic.Languages.Cpp;

/// <summary>
/// Direct unit tests for <see cref="CppLexer"/> targeting uncovered token paths.
/// </summary>
[TestClass]
public sealed class CppLexerTests
{
    [TestMethod]
    public void Tokenize_empty_source_returns_eof_only()
    {
        var tokens = new CppLexer(string.Empty).Tokenize();
        Assert.HasCount(1, tokens);
        Assert.AreEqual(CppTokenKind.EndOfFile, tokens[0].Kind);
    }

    [TestMethod]
    [DataRow("<<=", CppTokenKind.ShiftLeftEqual)]
    [DataRow(">>=", CppTokenKind.ShiftRightEqual)]
    public void Tokenize_three_char_compound_assign(string text, CppTokenKind kind)
    {
        var tokens = new CppLexer($"a {text} b;").Tokenize();
        Assert.IsTrue(tokens.Any(t => t.Kind == kind));
    }

    [TestMethod]
    [DataRow("::", CppTokenKind.ScopeResolution)]
    [DataRow("==", CppTokenKind.Equal)]
    [DataRow("!=", CppTokenKind.NotEqual)]
    [DataRow("<=", CppTokenKind.LessEqual)]
    [DataRow(">=", CppTokenKind.GreaterEqual)]
    [DataRow("<<", CppTokenKind.ShiftLeft)]
    [DataRow(">>", CppTokenKind.ShiftRight)]
    [DataRow("&&", CppTokenKind.LogicalAnd)]
    [DataRow("||", CppTokenKind.LogicalOr)]
    [DataRow("->", CppTokenKind.Arrow)]
    [DataRow("++", CppTokenKind.PlusPlus)]
    [DataRow("--", CppTokenKind.MinusMinus)]
    [DataRow("+=", CppTokenKind.PlusEqual)]
    [DataRow("-=", CppTokenKind.MinusEqual)]
    [DataRow("*=", CppTokenKind.StarEqual)]
    [DataRow("/=", CppTokenKind.SlashEqual)]
    [DataRow("%=", CppTokenKind.PercentEqual)]
    [DataRow("&=", CppTokenKind.AmpEqual)]
    [DataRow("|=", CppTokenKind.PipeEqual)]
    [DataRow("^=", CppTokenKind.CaretEqual)]
    public void Tokenize_two_char_operators(string text, CppTokenKind kind)
    {
        var tokens = new CppLexer($"a {text} b;").Tokenize();
        Assert.IsTrue(tokens.Any(t => t.Kind == kind), $"Expected {kind} for '{text}'");
    }

    [TestMethod]
    public void Tokenize_string_literal_with_escapes()
    {
        var tokens = new CppLexer("\"a\\nb\\tc\\r\\\\\\\"\\'\\0d\"").Tokenize();
        Assert.AreEqual(CppTokenKind.StringLiteral, tokens[0].Kind);
        Assert.Contains("\n", tokens[0].StringValue!);
        Assert.Contains("\t", tokens[0].StringValue!);
        Assert.Contains("\r", tokens[0].StringValue!);
        Assert.Contains("\0", tokens[0].StringValue!);
    }

    [TestMethod]
    public void Tokenize_char_literal_escapes()
    {
        var tokens = new CppLexer("'\\n'").Tokenize();
        Assert.AreEqual(CppTokenKind.CharLiteral, tokens[0].Kind);
        Assert.AreEqual('\n', tokens[0].CharValue);
    }

    [TestMethod]
    public void Tokenize_unterminated_string_throws()
    {
        Assert.ThrowsExactly<UnsupportedSyntaxException>(() => new CppLexer("\"abc").Tokenize());
    }

    [TestMethod]
    public void Tokenize_unterminated_char_throws()
    {
        Assert.ThrowsExactly<UnsupportedSyntaxException>(() => new CppLexer("'a").Tokenize());
    }

    [TestMethod]
    public void Tokenize_unknown_escape_throws()
    {
        Assert.ThrowsExactly<UnsupportedSyntaxException>(() => new CppLexer("\"\\q\"").Tokenize());
    }

    [TestMethod]
    public void Tokenize_skips_line_comment()
    {
        var tokens = new CppLexer("// hello\n42").Tokenize();
        Assert.AreEqual(CppTokenKind.IntegerLiteral, tokens[0].Kind);
    }

    [TestMethod]
    public void Tokenize_skips_block_comment()
    {
        var tokens = new CppLexer("/* hello */ 42").Tokenize();
        Assert.AreEqual(CppTokenKind.IntegerLiteral, tokens[0].Kind);
    }

    [TestMethod]
    public void Tokenize_skips_preprocessor_directive_line()
    {
        var tokens = new CppLexer("#include <stdio.h>\n42").Tokenize();
        Assert.AreEqual(CppTokenKind.IntegerLiteral, tokens[0].Kind);
    }

    [TestMethod]
    [DataRow("class", CppTokenKind.ClassKeyword)]
    [DataRow("public", CppTokenKind.PublicKeyword)]
    [DataRow("private", CppTokenKind.PrivateKeyword)]
    [DataRow("protected", CppTokenKind.ProtectedKeyword)]
    [DataRow("namespace", CppTokenKind.NamespaceKeyword)]
    [DataRow("using", CppTokenKind.UsingKeyword)]
    [DataRow("template", CppTokenKind.TemplateKeyword)]
    [DataRow("typename", CppTokenKind.TypenameKeyword)]
    [DataRow("new", CppTokenKind.NewKeyword)]
    [DataRow("delete", CppTokenKind.DeleteKeyword)]
    [DataRow("bool", CppTokenKind.BoolKeyword)]
    [DataRow("true", CppTokenKind.TrueKeyword)]
    [DataRow("false", CppTokenKind.FalseKeyword)]
    [DataRow("virtual", CppTokenKind.VirtualKeyword)]
    [DataRow("override", CppTokenKind.OverrideKeyword)]
    [DataRow("this", CppTokenKind.ThisKeyword)]
    [DataRow("nullptr", CppTokenKind.NullptrKeyword)]
    [DataRow("operator", CppTokenKind.OperatorKeyword)]
    [DataRow("friend", CppTokenKind.FriendKeyword)]
    [DataRow("inline", CppTokenKind.InlineKeyword)]
    public void Tokenize_cpp_keywords_resolve(string text, CppTokenKind kind)
    {
        var tokens = new CppLexer(text).Tokenize();
        Assert.AreEqual(kind, tokens[0].Kind);
    }

    [TestMethod]
    public void Tokenize_hex_integer_literal()
    {
        var tokens = new CppLexer("0xFF").Tokenize();
        Assert.AreEqual(CppTokenKind.IntegerLiteral, tokens[0].Kind);
    }
}
