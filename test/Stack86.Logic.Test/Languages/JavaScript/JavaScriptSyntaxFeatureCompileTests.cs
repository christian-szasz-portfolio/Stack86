namespace Stack86.Logic.Test.Languages.JavaScript;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// JavaScript programs targeting under-covered JsParser/JsLexer/JsIrGenerator paths.
/// </summary>
[TestClass]
public sealed class JavaScriptSyntaxFeatureCompileTests
{
    [TestMethod]
    [DataRow("const x = 0xFF;\nconst y = 0o17;\nconst z = 0b1010;\nconsole.log(x);\n", "numeric_radix")]
    [DataRow("let x = 1;\nx += 5;\nx -= 2;\nx *= 3;\nx /= 2;\nx %= 4;\nconsole.log(x);\n", "compound_assign")]
    [DataRow("const x = 0xFF;\nconst y = x & 0x0F;\nconst z = x | 0x10;\nconst w = x ^ 0x55;\nconsole.log(y);\n", "bitwise")]
    [DataRow("const x = 1 << 4;\nconst y = x >> 2;\nconsole.log(y);\n", "shifts")]
    [DataRow("const x = 5;\nconst y = -x;\nconst z = !true;\nconsole.log(y);\n", "unary_ops")]
    [DataRow("let x = 0;\nx++;\nx--;\n++x;\n--x;\nconsole.log(x);\n", "inc_dec")]
    [DataRow("const x = 5;\nif (x === 5) console.log('strict_eq');\nif (x !== 0) console.log('strict_ne');\n", "strict_equality")]
    [DataRow("let x = 0;\ndo { x++; } while (x < 5);\nconsole.log(x);\n", "do_while")]
    [DataRow("const x = 5;\nswitch (x) { case 1: console.log('a'); break; case 5: console.log('b'); break; default: console.log('c'); }\n", "switch")]
    [DataRow("for (let i = 0; i < 10; i++) { if (i === 5) break; if (i === 2) continue; console.log(i); }\n", "break_continue")]
    [DataRow("function fact(n) { return n <= 1 ? 1 : n * fact(n - 1); }\nconsole.log(fact(6));\n", "ternary_recursion")]
    [DataRow("const a = 'hello';\nconst b = 'world';\nconst c = a + ' ' + b;\nconsole.log(c);\n", "string_concat")]
    [DataRow("const s = 'esc\\nape\\t\\\\';\nconsole.log(s);\n", "string_escapes")]
    public async Task JsProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.JavaScript, source, $"{label}.js");

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
