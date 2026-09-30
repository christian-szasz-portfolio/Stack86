namespace Stack86.Logic.Test.Languages.JavaScript;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Programs targeting under-covered JsIrGenerator paths:
/// member access, object property store, update expressions, typeof,
/// simple/compound assignment, calls, template literals.
/// </summary>
[TestClass]
public sealed class JavaScriptIrGeneratorFeatureCompileTests
{
    [TestMethod]
    [DataRow("const o = { x: 1, y: 2 };\nconsole.log(o.x);\nconsole.log(o.y);\n", "object_member_access")]
    [DataRow("const o = { x: 0 };\no.x = 42;\nconsole.log(o.x);\n", "object_property_store")]
    [DataRow("let x = 10;\nx = 20;\nconsole.log(x);\n", "simple_assignment")]
    [DataRow("let x = 10;\nx += 5;\nx -= 2;\nx *= 3;\nx /= 2;\nx %= 4;\nconsole.log(x);\n", "compound_assignment")]
    [DataRow("let x = 0;\nx++;\nx--;\nconsole.log(x);\n", "post_update")]
    [DataRow("let x = 0;\n++x;\n--x;\nconsole.log(x);\n", "pre_update")]
    [DataRow("const x = 5;\nconsole.log(typeof x);\nconst s = 'hi';\nconsole.log(typeof s);\n", "typeof_multiple")]
    [DataRow("const a = [1, 2, 3];\nconsole.log(a[0]);\nconsole.log(a[1]);\nconsole.log(a[2]);\n", "array_access")]
    [DataRow("const a = [10, 20, 30];\na[1] = 99;\nconsole.log(a[1]);\n", "array_store")]
    [DataRow("function add(a, b) { return a + b; }\nconsole.log(add(2, 3));\nconsole.log(add(10, 20));\n", "call_multiple")]
    [DataRow("const x = 5;\nconst y = 7;\nconsole.log(`sum=${x + y}, prod=${x * y}`);\n", "template_multi_expr")]
    [DataRow("const o = { name: 'Alice', age: 30 };\nconsole.log(`${o.name} is ${o.age}`);\n", "template_member")]
    [DataRow("function counter() { let n = 0; return function() { n++; return n; }; }\nconst c = counter();\nconsole.log(c());\nconsole.log(c());\n", "closure")]
    [DataRow("const a = [1, 2, 3];\nlet s = 0;\nfor (let i = 0; i < a.length; i++) s += a[i];\nconsole.log(s);\n", "array_length")]
    public async Task JsProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.JavaScript, source, $"{label}.js");

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
