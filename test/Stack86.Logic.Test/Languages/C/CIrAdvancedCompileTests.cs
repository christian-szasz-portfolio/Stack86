namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// C programs targeting CIrGenerator advanced paths:
/// scanf, puts, printf format variants, struct member-address.
/// </summary>
[TestClass]
public sealed class CIrAdvancedCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nint main() { int x; scanf(\"%d\", &x); printf(\"%d\", x); return 0; }", "scanf_int")]
    [DataRow("#include <stdio.h>\nint main() { int x; int y; scanf(\"%d %d\", &x, &y); printf(\"%d\", x + y); return 0; }", "scanf_two_int")]
    [DataRow("#include <stdio.h>\nint main() { char c; scanf(\"%c\", &c); printf(\"%c\", c); return 0; }", "scanf_char")]
    [DataRow("#include <stdio.h>\nint main() { puts(\"hello\"); return 0; }", "puts")]
    [DataRow("#include <stdio.h>\nint main() { puts(\"a\"); puts(\"b\"); puts(\"c\"); return 0; }", "puts_multi")]
    [DataRow("#include <stdio.h>\nint main() { int a = 5; printf(\"%d\\n\", a); return 0; }", "printf_int_newline")]
    [DataRow("#include <stdio.h>\nint main() { char c = 'X'; printf(\"%c\", c); return 0; }", "printf_char")]
    [DataRow("#include <stdio.h>\nint main() { printf(\"%s\", \"world\"); return 0; }", "printf_str")]
    [DataRow("#include <stdio.h>\nint main() { int a = 1, b = 2; printf(\"%d %d\\n\", a, b); return 0; }", "printf_two_int")]
    [DataRow("#include <stdio.h>\nint main() { printf(\"a=%d b=%d c=%d\", 1, 2, 3); return 0; }", "printf_three_int")]
    [DataRow("#include <stdio.h>\nstruct P { int x; int y; };\nint main() { struct P p; p.x = 10; p.y = 20; printf(\"%d\", p.x + p.y); return 0; }", "struct_member")]
    [DataRow("#include <stdio.h>\nstruct P { int x; int y; };\nint main() { struct P p; scanf(\"%d\", &p.x); printf(\"%d\", p.x); return 0; }", "struct_member_addr")]
    [DataRow("#include <stdio.h>\nint main() { int a[5]; for (int i = 0; i < 5; i++) a[i] = i * 2; for (int j = 0; j < 5; j++) printf(\"%d \", a[j]); return 0; }", "array_iterate")]
    [DataRow("#include <stdio.h>\nint main() { int a = 5, b = 7; int *p = &a; printf(\"%d\", *p); p = &b; printf(\"%d\", *p); return 0; }", "pointer_swap")]
    [DataRow("#include <stdio.h>\nvoid greet(const char *s) { printf(\"%s\", s); }\nint main() { greet(\"hi\"); return 0; }", "func_str_param")]
    [DataRow("#include <stdio.h>\nint sq(int x) { return x * x; }\nint main() { for (int i = 1; i <= 5; i++) printf(\"%d \", sq(i)); return 0; }", "func_call_in_loop")]
    [DataRow("#include <stdio.h>\nint main() { int n = 10; int sum = 0; for (int i = 1; i <= n; i++) sum += i; printf(\"%d\", sum); return 0; }", "sum_loop")]
    public async Task CProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileC(source);

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
