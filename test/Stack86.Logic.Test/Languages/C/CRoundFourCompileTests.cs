namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Round 4 C programs targeting deep CIrGenerator paths:
/// scanf %s, scanf error path, putchar, puts(variable), arrow access,
/// struct member address variants, union access.
/// </summary>
[TestClass]
public sealed class CRoundFourCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nint main() { char buf[16]; scanf(\"%s\", buf); printf(\"%s\", buf); return 0; }", "scanf_str")]
    [DataRow("#include <stdio.h>\nint main() { putchar('A'); putchar('B'); putchar('\\n'); return 0; }", "putchar")]
    [DataRow("#include <stdio.h>\nint main() { putchar('X'); return 0; }", "putchar_single")]
    [DataRow("#include <stdio.h>\nint main() { const char *s = \"hello\"; puts(s); return 0; }", "puts_var")]
    [DataRow("#include <stdio.h>\nstruct P { int x; int y; int z; };\nint main() { struct P p; struct P *q = &p; q->x = 1; q->y = 2; q->z = 3; printf(\"%d %d %d\", q->x, q->y, q->z); return 0; }", "arrow_member")]
    [DataRow("#include <stdio.h>\nstruct P { int a; int b; int c; };\nint main() { struct P p; p.a = 10; p.b = 20; p.c = 30; printf(\"%d %d %d\", p.a, p.b, p.c); return 0; }", "dot_member_three")]
    [DataRow("#include <stdio.h>\nstruct P { int x; };\nint main() { struct P p; p.x = 42; struct P *q = &p; printf(\"%d\", q->x); return 0; }", "arrow_first_field")]
    [DataRow("#include <stdio.h>\nstruct P { int x; int y; };\nint main() { struct P p; struct P *q = &p; scanf(\"%d\", &q->x); printf(\"%d\", q->x); return 0; }", "scanf_arrow_field")]
    [DataRow("#include <stdio.h>\nint main() { int n; scanf(\"%d\", &n); int sum = 0; for (int i = 1; i <= n; i++) sum += i; printf(\"%d\", sum); return 0; }", "scanf_then_loop")]
    [DataRow("#include <stdio.h>\nint main() { char c; scanf(\"%c\", &c); putchar(c); putchar(c); return 0; }", "scanf_char_then_putchar")]
    [DataRow("#include <stdio.h>\nint main() { puts(\"line1\"); puts(\"line2\"); puts(\"line3\"); return 0; }", "many_puts")]
    [DataRow("#include <stdio.h>\nint main() { for (int i = 'a'; i <= 'e'; i++) putchar(i); putchar('\\n'); return 0; }", "putchar_in_loop")]
    [DataRow("#include <stdio.h>\nint absval(int x) { return x < 0 ? -x : x; }\nint main() { printf(\"%d %d %d\", absval(-5), absval(0), absval(7)); return 0; }", "abs_func")]
    [DataRow("#include <stdio.h>\nint main() { int a = 1, b = 2; int *p = &a; *p = 99; printf(\"%d %d\", a, b); return 0; }", "deref_assign")]
    [DataRow("#include <stdio.h>\nstruct Pt { int x; int y; };\nvoid setX(struct Pt *p, int v) { p->x = v; }\nint main() { struct Pt p; p.y = 0; setX(&p, 88); printf(\"%d\", p.x); return 0; }", "func_struct_ptr")]
    public async Task CProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileC(source);

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
