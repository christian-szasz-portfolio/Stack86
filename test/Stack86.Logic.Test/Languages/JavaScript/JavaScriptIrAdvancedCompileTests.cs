namespace Stack86.Logic.Test.Languages.JavaScript;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// JavaScript programs targeting JsIrGenerator advanced paths:
/// member access, object property stores, typeof, compound assignment.
/// </summary>
[TestClass]
public sealed class JavaScriptIrAdvancedCompileTests
{
    [TestMethod]
    [DataRow("let s = 'hello';\nconsole.log(s.length);\n", "string_length")]
    [DataRow("let a = [1, 2, 3, 4, 5];\nconsole.log(a.length);\n", "array_length")]
    [DataRow("let a = [10, 20, 30];\na[1] = 99;\nconsole.log(a[1]);\n", "array_index_store")]
    [DataRow("let o = { x: 1, y: 2 };\nconsole.log(o.x);\nconsole.log(o.y);\n", "object_property_read")]
    [DataRow("let o = { a: 1 };\no.a = 42;\nconsole.log(o.a);\n", "object_property_store")]
    [DataRow("let o = { a: 0, b: 0 };\no.a = 5;\no.b = 10;\nconsole.log(o.a + o.b);\n", "object_two_field_store")]
    [DataRow("let x = 10;\nx += 5;\nconsole.log(x);\n", "compound_add_assign")]
    [DataRow("let x = 10;\nx -= 3;\nconsole.log(x);\n", "compound_sub_assign")]
    [DataRow("let x = 4;\nx *= 3;\nconsole.log(x);\n", "compound_mul_assign")]
    [DataRow("let x = 12;\nx /= 4;\nconsole.log(x);\n", "compound_div_assign")]
    [DataRow("let x = 17;\nx %= 5;\nconsole.log(x);\n", "compound_mod_assign")]
    [DataRow("let x = 5;\nconsole.log(typeof x);\n", "typeof_number")]
    [DataRow("let s = 'hi';\nconsole.log(typeof s);\n", "typeof_string")]
    [DataRow("let b = true;\nconsole.log(typeof b);\n", "typeof_bool")]
    [DataRow("let a = [1,2,3];\nconsole.log(typeof a);\n", "typeof_array")]
    [DataRow("function add(a, b) { return a + b; }\nconsole.log(add(3, 4));\n", "func_decl")]
    [DataRow("let f = function(x) { return x * 2; };\nconsole.log(f(21));\n", "func_expr")]
    [DataRow("let f = (x) => x * 3;\nconsole.log(f(7));\n", "arrow_func")]
    [DataRow("let x = 0;\nfor (let i = 0; i < 5; i++) { x += i; }\nconsole.log(x);\n", "for_loop")]
    [DataRow("let x = 10;\nlet y = (x > 5) ? 100 : 200;\nconsole.log(y);\n", "ternary")]
    public async Task JavaScriptProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.JavaScript, source, $"{label}.js");

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
