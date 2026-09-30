namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Languages.C;
using Stack86.Logic.Pipeline.Ir;
using Stack86.Logic.Pipeline.X86Conversion;

/// <summary>
/// Unit tests for <see cref="CCapabilityValidator"/> covering rejection of unsupported
/// type/qualifier/statement keywords and false-positive avoidance inside strings/comments.
/// </summary>
[TestClass]
public sealed class CCapabilityValidatorTests
{
    [TestMethod]
    public void Validate_EmptySource_ReturnsNoDiagnostics()
    {
        Assert.HasCount(0, Create().Validate(string.Empty));
    }

    [TestMethod]
    public void Validate_OnlySupportedTypes_ReturnsNoDiagnostics()
    {
        const string source = """
            int main(void) {
                int x = 1;
                char c = 'a';
                return x;
            }
            """;

        Assert.HasCount(0, Create().Validate(source));
    }

    [TestMethod]
    [DataRow("float")]
    [DataRow("double")]
    [DataRow("long")]
    [DataRow("short")]
    [DataRow("signed")]
    [DataRow("unsigned")]
    public void Validate_UnsupportedTypeKeyword_ReportsError(string keyword)
    {
        var source = $"int main(void) {{ {keyword} x; return 0; }}";

        var diags = Create().Validate(source);

        Assert.HasCount(1, diags);
        Assert.AreEqual(DiagnosticSeverity.Error, diags[0].Severity);
        Assert.Contains($"Type '{keyword}'", diags[0].Message);
        Assert.Contains("8086", diags[0].Message);
        Assert.AreEqual(1, diags[0].Line);
    }

    [TestMethod]
    [DataRow("const")]
    [DataRow("volatile")]
    [DataRow("register")]
    [DataRow("restrict")]
    public void Validate_UnsupportedQualifier_ReportsError(string keyword)
    {
        var source = $"int main(void) {{ {keyword} int x = 1; return x; }}";

        var diags = Create().Validate(source);

        Assert.IsGreaterThanOrEqualTo(1, diags.Count);
        Assert.IsTrue(diags.Any(d => d.Message.Contains($"Qualifier '{keyword}'", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Validate_GotoStatement_ReportsError()
    {
        const string source = """
            int main(void) {
                goto end;
            end:
                return 0;
            }
            """;

        var diags = Create().Validate(source);

        Assert.IsTrue(diags.Any(d => d.Message.Contains("'goto'", StringComparison.Ordinal)));
        Assert.AreEqual(2, diags.First(d => d.Message.Contains("goto", StringComparison.Ordinal)).Line);
    }

    [TestMethod]
    public void Validate_KeywordInsideStringLiteral_IsIgnored()
    {
        const string source = """
            int main(void) { char *s = "float and double"; return 0; }
            """;

        Assert.HasCount(0, Create().Validate(source));
    }

    [TestMethod]
    public void Validate_KeywordInsideCharLiteral_IsIgnored()
    {
        // Char literal cannot contain "float" but verify quoting stripping does not crash.
        const string source = "int main(void) { char c = 'f'; return 0; }";

        Assert.HasCount(0, Create().Validate(source));
    }

    [TestMethod]
    public void Validate_KeywordInsideLineComment_IsIgnored()
    {
        const string source = """
            int main(void) {
                // float is not allowed but this is a comment
                return 0;
            }
            """;

        Assert.HasCount(0, Create().Validate(source));
    }

    [TestMethod]
    public void Validate_KeywordInsideSingleLineBlockComment_IsIgnored()
    {
        const string source = "int main(void) { /* float is fine here */ return 0; }";

        Assert.HasCount(0, Create().Validate(source));
    }

    [TestMethod]
    public void Validate_PreprocessorDirective_IsSkipped()
    {
        // A #define line that mentions 'long' should not emit a diagnostic for that line.
        const string source = """
            #define LONG_VALUE long
            int main(void) { return 0; }
            """;

        var diags = Create().Validate(source);

        Assert.HasCount(0, diags);
    }

    [TestMethod]
    public void Validate_MultipleErrorsOnDifferentLines_AllReported()
    {
        const string source = """
            int main(void) {
                float x;
                double y;
                goto end;
            end:
                return 0;
            }
            """;

        var diags = Create().Validate(source);

        Assert.IsGreaterThanOrEqualTo(3, diags.Count);
        Assert.IsTrue(diags.Any(d => d.Line == 2 && d.Message.Contains("float", StringComparison.Ordinal)));
        Assert.IsTrue(diags.Any(d => d.Line == 3 && d.Message.Contains("double", StringComparison.Ordinal)));
        Assert.IsTrue(diags.Any(d => d.Line == 4 && d.Message.Contains("goto", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Validate_StringLiteralWithEscapedQuote_DoesNotMisparse()
    {
        // Escaped quote should not break stripping; 'float' inside the string is ignored.
        const string source = "int main(void) { char *s = \"a\\\"float\\\"b\"; return 0; }";

        Assert.HasCount(0, Create().Validate(source));
    }

    [TestMethod]
    public void Validate_CharLiteralWithEscapedQuote_DoesNotMisparse()
    {
        const string source = "int main(void) { char c = '\\''; return 0; }";

        Assert.HasCount(0, Create().Validate(source));
    }

    private static CCapabilityValidator Create() => new(new X8086CapabilityProfile());
}
