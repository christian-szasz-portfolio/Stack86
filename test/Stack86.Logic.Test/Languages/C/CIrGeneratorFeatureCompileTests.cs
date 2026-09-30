namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Test.Compilation;

/// <summary>
/// C programs targeting under-covered CIrGenerator paths:
/// scanf variants, puts, member address (struct field addr), printf format specifiers,
/// var declaration patterns, address-of, pre-inc/dec.
/// </summary>
[TestClass]
public sealed class CIrGeneratorFeatureCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nint main() { int x; scanf(\"%d\", &x); printf(\"%d\\n\", x); return 0; }", "scanf_printf_int")]
    [DataRow("#include <stdio.h>\nint main() { char c; scanf(\"%c\", &c); printf(\"%c\\n\", c); return 0; }", "scanf_printf_char")]
    [DataRow("#include <stdio.h>\nint main() { int a; int b; scanf(\"%d %d\", &a, &b); printf(\"%d\", a + b); return 0; }", "scanf_two_ints")]
    [DataRow("#include <stdio.h>\nint main() { puts(\"line1\"); puts(\"line2\"); return 0; }", "puts_multiple")]
    [DataRow("#include <stdio.h>\nstruct P { int x; int y; };\nint main() { struct P p; p.x = 1; p.y = 2; printf(\"%d %d\", p.x, p.y); return 0; }", "struct_member")]
    [DataRow("#include <stdio.h>\nstruct P { int x; int y; };\nint main() { struct P p; int* px = &p.x; *px = 99; printf(\"%d\", p.x); return 0; }", "struct_member_address")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int* p = &x; *p = 42; printf(\"%d\", x); return 0; }", "address_of_var")]
    [DataRow("#include <stdio.h>\nint main() { int x = 0; ++x; --x; ++x; printf(\"%d\", x); return 0; }", "pre_inc_dec")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; printf(\"%d %d %d\", x, x*2, x*3); return 0; }", "printf_multi_arg")]
    [DataRow("#include <stdio.h>\nint main() { int a = 1; int b = 2; int c = 3; printf(\"%d\", a + b + c); return 0; }", "multi_var_decl_separate")]
    [DataRow("#include <stdio.h>\nint main() { int a[3]; a[0] = 1; a[1] = 2; a[2] = 3; printf(\"%d\", a[0] + a[1] + a[2]); return 0; }", "array_indexed")]
    [DataRow("#include <stdio.h>\nint main() { for (int i = 0; i < 5; i++) { printf(\"%d \", i); } return 0; }", "for_print")]
    [DataRow("#include <stdio.h>\nint main() { int x = 10; while (x > 0) { printf(\"%d \", x); x--; } return 0; }", "while_print")]
    public async Task CProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileC(source);

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
