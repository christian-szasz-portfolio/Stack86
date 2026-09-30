namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Round-ten C compile tests targeting <c>CParser</c>/<c>CIrGenerator</c> remaining gaps.
/// </summary>
[TestClass]
public sealed class CRoundTenCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nstruct P { int x; int y; };\nint main() { struct P p; p.x = 3; p.y = 4; printf(\"%d\", p.x + p.y); return 0; }", "struct_field_assign")]
    [DataRow("#include <stdio.h>\nstruct P { int v; };\nint sum(struct P a, struct P b) { return a.v + b.v; }\nint main() { struct P x; struct P y; x.v = 5; y.v = 6; printf(\"%d\", sum(x, y)); return 0; }", "struct_value_param")]
    [DataRow("#include <stdio.h>\nstruct P { int v; };\nint get(struct P *p) { return p->v; }\nint main() { struct P p; p.v = 11; printf(\"%d\", get(&p)); return 0; }", "struct_pointer_arrow")]
    [DataRow("#include <stdio.h>\nint main() { int a[3]; a[0] = 10; a[1] = 20; a[2] = 30; printf(\"%d\", a[0] + a[1] + a[2]); return 0; }", "array_assign_sum")]
    [DataRow("#include <stdio.h>\nint main() { int x = 10; int *p = &x; *p = 99; printf(\"%d\", x); return 0; }", "ptr_deref_assign")]
    [DataRow("#include <stdio.h>\nvoid set(int *p, int v) { *p = v; }\nint main() { int x = 0; set(&x, 77); printf(\"%d\", x); return 0; }", "ptr_param_assign")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; switch (x) { case 1: printf(\"1\"); break; case 5: printf(\"5\"); break; default: printf(\"d\"); } return 0; }", "switch_case_break")]
    [DataRow("#include <stdio.h>\nint main() { int x = 3; int y = x++; int z = ++x; printf(\"%d %d %d\", x, y, z); return 0; }", "post_pre_inc")]
    [DataRow("#include <stdio.h>\nint main() { int x = 10; x += 5; x -= 2; x *= 2; x /= 3; printf(\"%d\", x); return 0; }", "compound_assign")]
    [DataRow("#include <stdio.h>\nint main() { int a = 0xFF; int b = a & 0x0F; int c = a | 0x10; int d = a ^ 0x55; printf(\"%d %d %d\", b, c, d); return 0; }", "bitwise")]
    [DataRow("#include <stdio.h>\nint main() { int a = 1; int b = a << 4; int c = b >> 2; printf(\"%d %d\", b, c); return 0; }", "shifts")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int y = (x > 0) ? 100 : -100; printf(\"%d\", y); return 0; }", "ternary")]
    [DataRow("#include <stdio.h>\nint main() { int x = 0; do { x = x + 1; printf(\"%d\", x); } while (x < 3); return 0; }", "do_while")]
    [DataRow("#include <stdio.h>\nint main() { for (int i = 0; i < 5; i++) { if (i == 1) continue; if (i == 4) break; printf(\"%d\", i); } return 0; }", "for_break_continue")]
    [DataRow("#include <stdio.h>\nint fib(int n) { if (n < 2) return n; return fib(n-1) + fib(n-2); }\nint main() { for (int i = 0; i < 7; i++) printf(\"%d\", fib(i)); return 0; }", "fib_recursion")]
    [DataRow("#include <stdio.h>\nenum Color { RED, GREEN, BLUE };\nint main() { enum Color c = GREEN; printf(\"%d\", c); return 0; }", "enum_decl")]
    [DataRow("#include <stdio.h>\nint main() { int a = 5, b = 10, c = 15; printf(\"%d\", a + b + c); return 0; }", "comma_init")]
    public async Task Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.C, source, $"{label}.c");
        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
