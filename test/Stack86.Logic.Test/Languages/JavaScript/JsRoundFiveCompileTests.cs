namespace Stack86.Logic.Test.Languages.JavaScript;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Round-five JavaScript compile tests targeting <c>JsIrGenerator</c> gaps.
/// </summary>
[TestClass]
public sealed class JsRoundFiveCompileTests
{
    [TestMethod]
    [DataRow("let x = 5; if (x > 0) { console.log('pos'); } else if (x < 0) { console.log('neg'); } else { console.log('zero'); }", "if_else_chain")]
    [DataRow("let total = 0; for (let i = 0; i < 5; i++) { total = total + i; } console.log(total);", "for_loop_sum")]
    [DataRow("let i = 0; while (i < 4) { console.log(i); i++; }", "while_loop")]
    [DataRow("let i = 0; do { console.log(i); i++; } while (i < 3);", "do_while")]
    [DataRow("for (let i = 0; i < 5; i++) { if (i == 2) continue; if (i == 4) break; console.log(i); }", "break_continue")]
    [DataRow("function fib(n) { if (n < 2) return n; return fib(n-1) + fib(n-2); } for (let i = 0; i < 6; i++) console.log(fib(i));", "fib")]
    [DataRow("let a = [1, 2, 3, 4, 5]; let s = 0; for (let i = 0; i < a.length; i++) s = s + a[i]; console.log(s);", "array_loop_sum")]
    [DataRow("let a = []; a[0] = 10; a[1] = 20; a[2] = 30; console.log(a[0] + a[1] + a[2]);", "array_index_store_load")]
    [DataRow("let p = { x: 3, y: 4 }; p.x = 10; p.y = 20; console.log(p.x + p.y);", "object_field_store_load")]
    [DataRow("let x = 5; let y = (x > 0) ? 1 : -1; console.log(y);", "ternary")]
    [DataRow("let a = true; let b = false; let c = a && b; let d = a || b; let e = !a; console.log(c); console.log(d); console.log(e);", "logical_ops")]
    [DataRow("let a = 1; a += 5; a -= 1; a *= 3; a /= 2; a %= 4; console.log(a);", "compound_arith")]
    [DataRow("function sq(x) { return x * x; } function cu(x) { return x * x * x; } console.log(sq(4)); console.log(cu(3));", "two_funcs")]
    [DataRow("const sq = x => x * x; const cu = (x) => x * x * x; console.log(sq(5)); console.log(cu(2));", "arrow_one_two")]
    [DataRow("const add = (a, b) => a + b; const sub = (a, b) => a - b; console.log(add(7, 3)); console.log(sub(10, 4));", "arrow_two_arg")]
    [DataRow("function f() { return 42; } let x = f(); console.log(x);", "func_no_arg_return")]
    [DataRow("let a = 5; let b = a; a = 10; console.log(a); console.log(b);", "value_copy")]
    [DataRow("let s = 'hello'; let n = s.length; console.log(n);", "string_length")]
    public async Task Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.JavaScript, source, $"{label}.js");
        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
