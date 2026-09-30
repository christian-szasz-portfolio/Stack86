namespace Stack86.Logic.Test.Languages.JavaScript;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Targeted JavaScript compilation tests for features missed by SPA samples:
/// template literals, typeof, function expressions, arrow functions, spread.
/// </summary>
[TestClass]
public sealed class JavaScriptFeatureCompileTests
{
    [TestMethod]
    [DataRow("const x = 5;\nconsole.log(`value=${x}`);\n", "template_literal")]
    [DataRow("const x = 5;\nconsole.log(typeof x);\n", "typeof")]
    [DataRow("const add = function(a, b) { return a + b; };\nconsole.log(add(2,3));\n", "function_expression")]
    [DataRow("const sq = (x) => x * x;\nconsole.log(sq(4));\n", "arrow")]
    [DataRow("function fact(n) { if (n <= 1) return 1; return n * fact(n-1); }\nconsole.log(fact(5));\n", "recursion")]
    [DataRow("let x = 0; for (let i = 0; i < 5; i++) x += i; console.log(x);\n", "for_loop")]
    [DataRow("let x = 0; while (x < 5) x++; console.log(x);\n", "while_loop")]
    [DataRow("let a = [1,2,3]; for (const v of a) console.log(v);\n", "for_of")]
    [DataRow("const x = true; if (x && !false) console.log(1);\n", "bool_logic")]
    [DataRow("let x = 10; let y = (x > 5) ? 1 : 2; console.log(y);\n", "ternary")]
    [DataRow("const a = 5; const b = a + 3; const c = b * 2; console.log(c);\n", "arithmetic")]
    public async Task JavaScriptProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.JavaScript, source, $"{label}.js");

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
