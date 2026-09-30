namespace Stack86.Logic.Test.Languages.JavaScript;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Targets remaining uncovered <c>JsParser</c>/<c>JsIrGenerator</c> branches:
/// for-loop initialiser with let/const, for-of with let/const, continue,
/// comma operator, compound assignments, shift operators, strict equality,
/// unary +/~, ternary, template literal interpolation, function expression,
/// arrow function, array/object trailing comma, object shorthand, null/undefined literals.
/// </summary>
[TestClass]
public sealed class JsRoundFourteenCompileTests
{
    [TestMethod]
    [DataRow("for (let i = 0; i < 3; i = i + 1) { console.log(i); }", "for_let")]
    [DataRow("for (const k = 0; k < 1; k = k + 1) { console.log(k); }", "for_const")]
    [DataRow("for (let i = 0; i < 5; i = i + 1) { if (i == 2) continue; console.log(i); }", "for_continue")]
    [DataRow("let arr = [1, 2, 3]; for (const x of arr) { console.log(x); }", "for_of_const")]
    [DataRow("let a = 5; a += 3; a -= 1; a *= 2; a /= 1; a %= 5; console.log(a);", "compound_arith")]
    [DataRow("let a = 0xFF; a &= 0x0F; a |= 0x10; a ^= 0xFF; console.log(a);", "compound_bitwise")]
    [DataRow("let a = 1; a <<= 4; a >>= 1; console.log(a);", "compound_shift")]
    [DataRow("let a = 5; let b = a << 2; let c = b >> 1; console.log(b + c);", "shift_expr")]
    [DataRow("let a = 1; let b = 1; if (a === b) console.log(\"eq\"); if (a !== 2) console.log(\"ne\");", "strict_eq")]
    [DataRow("let a = 5; let b = -a; let c = +a; let d = ~a; let e = !true; console.log(b + c + d);", "unary_ops")]
    [DataRow("let a = 5; let b = a > 0 ? 1 : 0; console.log(b);", "ternary")]
    [DataRow("let n = 7; let s = `value=${n}`; console.log(s);", "template_interp")]
    [DataRow("let f = function(a, b) { return a + b; }; console.log(f(2, 3));", "function_expr")]
    [DataRow("let f = function named(a) { return a; }; console.log(f(7));", "function_expr_named")]
    [DataRow("let add = (a, b) => a + b; console.log(add(4, 5));", "arrow")]
    [DataRow("let arr = [1, 2, 3,]; console.log(arr[0]);", "array_trailing_comma")]
    [DataRow("let obj = { x: 1, y: 2, }; console.log(obj.x);", "object_trailing_comma")]
    [DataRow("let x = 1; let obj = { x }; console.log(obj.x);", "object_shorthand")]
    [DataRow("let a = null; let b = undefined; let c = false; if (c == false) console.log(0);", "null_undef")]
    [DataRow("let i = 0;; i = i + 1; console.log(i);", "empty_stmt")]
    public async Task Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.JavaScript, source, $"{label}.js");
        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
