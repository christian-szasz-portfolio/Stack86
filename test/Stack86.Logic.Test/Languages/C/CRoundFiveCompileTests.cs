namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Round-five C compile tests targeting <c>CIrGenerator</c> and <c>CLexer</c> gaps.
/// </summary>
[TestClass]
public sealed class CRoundFiveCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nint main() { int a = 0xFF; int b = 0x10; int c = a & b; printf(\"%d\", c); return 0; }", "hex_and")]
    [DataRow("#include <stdio.h>\nint main() { int a = 0xFF; int b = a | 0x100; int c = a ^ 0x55; int d = ~a; printf(\"%d %d %d\", b, c, d); return 0; }", "hex_or_xor_not")]
    [DataRow("#include <stdio.h>\nint main() { int a = 1; int b = a << 4; int c = b >> 2; printf(\"%d %d\", b, c); return 0; }", "shifts")]
    [DataRow("#include <stdio.h>\nint main() { int a = 100; int b = -a; printf(\"%d\", b); return 0; }", "unary_minus")]
    [DataRow("#include <stdio.h>\nint main() { int a = 5; if (!(a == 0)) printf(\"nz\"); return 0; }", "logical_not")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; x++; ++x; x--; --x; printf(\"%d\", x); return 0; }", "pre_post_inc_dec")]
    [DataRow("#include <stdio.h>\nint main() { int x = 1; x += 5; x -= 1; x *= 3; x /= 2; x %= 4; printf(\"%d\", x); return 0; }", "compound_arith")]
    [DataRow("#include <stdio.h>\nint main() { int a = 0xFF; a &= 0x0F; a |= 0x10; a ^= 0x05; a <<= 1; a >>= 1; printf(\"%d\", a); return 0; }", "compound_bitwise_shift")]
    [DataRow("#include <stdio.h>\nint main() { for (int i = 0; i < 5; i++) { if (i == 2) continue; if (i == 4) break; printf(\"%d\", i); } return 0; }", "for_break_continue")]
    [DataRow("#include <stdio.h>\nint main() { int i = 0; while (i < 10) { i++; if (i == 5) break; } printf(\"%d\", i); return 0; }", "while_break")]
    [DataRow("#include <stdio.h>\nint main() { int i = 0; do { i++; } while (i < 3); printf(\"%d\", i); return 0; }", "do_while")]
    [DataRow("#include <stdio.h>\nint main() { int x = 3; switch (x) { case 1: printf(\"a\"); break; case 2: printf(\"b\"); break; case 3: printf(\"c\"); break; default: printf(\"d\"); } return 0; }", "switch_case_default")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int y = (x > 0) ? x : -x; printf(\"%d\", y); return 0; }", "ternary")]
    [DataRow("#include <stdio.h>\nint main() { int a = 1; int b = 2; int c = (a < b) && (b > 0); int d = (a > b) || (a == 1); printf(\"%d %d\", c, d); return 0; }", "logical_and_or_short_circuit")]
    [DataRow("#include <stdio.h>\nint g = 42; int main() { printf(\"%d\", g); return 0; }", "global_var")]
    [DataRow("#include <stdio.h>\nint sq(int x) { return x * x; } int cube(int x) { return x * x * x; }\nint main() { printf(\"%d %d\", sq(4), cube(3)); return 0; }", "two_funcs")]
    [DataRow("#include <stdio.h>\nint fib(int n) { if (n < 2) return n; return fib(n-1) + fib(n-2); }\nint main() { for (int i = 0; i < 8; i++) printf(\"%d \", fib(i)); return 0; }", "fib_recursion")]
    [DataRow("#include <stdio.h>\nint main() { char c = 'A'; printf(\"%c\", c); return 0; }", "char_literal")]
    [DataRow("#include <stdio.h>\nint main() { char c1 = '\\n'; char c2 = '\\t'; char c3 = '\\\\'; char c4 = '\\''; printf(\"%c%c%c%c\", c1, c2, c3, c4); return 0; }", "char_escapes")]
    public async Task Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.C, source, $"{label}.c");
        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
