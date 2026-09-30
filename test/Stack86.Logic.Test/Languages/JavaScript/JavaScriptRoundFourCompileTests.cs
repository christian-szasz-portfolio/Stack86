namespace Stack86.Logic.Test.Languages.JavaScript;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Round 4 JS programs targeting JsIrGenerator deep paths:
/// nested object member access (chained), simple/compound assignments,
/// typeof on multiple value types, function calls.
/// </summary>
[TestClass]
public sealed class JavaScriptRoundFourCompileTests
{
    [TestMethod]
    [DataRow("let r = { origin: { x: 1, y: 2 }, size: { x: 10, y: 20 } };\nconsole.log(r.origin.x);\nconsole.log(r.origin.y);\nconsole.log(r.size.x);\n", "nested_object_chained")]
    [DataRow("let p = { pos: { x: 5, y: 7 } };\np.pos.x = 99;\nconsole.log(p.pos.x);\n", "nested_object_store")]
    [DataRow("let o = { a: { b: 1 } };\nconsole.log(o.a.b);\n", "two_level_chain")]
    [DataRow("let p = { x: 0, y: 0, z: 0 };\np.x = 1;\np.y = 2;\np.z = 3;\nconsole.log(p.x + p.y + p.z);\n", "three_field_assign")]
    [DataRow("let x = 5; let y = 10; let z = x; x = y; y = z; console.log(x); console.log(y);\n", "swap_via_temp")]
    [DataRow("let counter = 0;\nfor (let i = 0; i < 100; i++) { counter += i; }\nconsole.log(counter);\n", "loop_compound")]
    [DataRow("function fact(n) { if (n <= 1) return 1; return n * fact(n-1); }\nconsole.log(fact(5));\n", "recursion")]
    [DataRow("function add(a, b) { return a + b; }\nfunction sub(a, b) { return a - b; }\nconsole.log(add(sub(10, 3), 5));\n", "func_compose")]
    [DataRow("let n = 0;\nlet s = 'hello';\nlet b = true;\nlet a = [1,2,3];\nlet o = { x: 1 };\nconsole.log(typeof n);\nconsole.log(typeof s);\nconsole.log(typeof b);\nconsole.log(typeof a);\nconsole.log(typeof o);\n", "typeof_all")]
    [DataRow("let arr = [10, 20, 30, 40, 50];\nfor (let i = 0; i < arr.length; i++) { arr[i] *= 2; }\nfor (let i = 0; i < arr.length; i++) { console.log(arr[i]); }\n", "array_double_in_place")]
    [DataRow("let x = 100;\nx /= 2;\nx *= 3;\nx -= 50;\nx %= 7;\nconsole.log(x);\n", "compound_chain")]
    [DataRow("let f = (a, b, c) => a + b * c;\nconsole.log(f(1, 2, 3));\nconsole.log(f(10, 20, 30));\n", "arrow_three_arg")]
    [DataRow("function square(x) { return x * x; }\nfor (let i = 1; i <= 5; i++) { console.log(square(i)); }\n", "func_call_in_loop")]
    [DataRow("let sum = 0;\nlet i = 0;\nwhile (i < 50) { sum += i; i++; }\nconsole.log(sum);\n", "while_loop")]
    public async Task JavaScriptProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.JavaScript, source, $"{label}.js");

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
