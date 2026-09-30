namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Round-fifteen C compile feature pressure for additional <c>CIrGenerator</c>/
/// <c>CParser</c> coverage: pre/post-increment, pre/post-decrement, address-of
/// and dereference, struct fields, sizeof, casts, switch with default, do-while,
/// unary operators, mixed pointer arithmetic, multiple-arg printf format specs.
/// </summary>
[TestClass]
public sealed class CRoundFifteenCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nint main() { int a = 5; int b = ++a; int c = --b; printf(\"%d\\n\", c); return 0; }\n", "preinc_predec")]
    [DataRow("#include <stdio.h>\nint main() { int a = 5; int b = a++; int c = b--; printf(\"%d\\n\", c); return 0; }\n", "postinc_postdec")]
    [DataRow("#include <stdio.h>\nint main() { int a = 7; int *p = &a; int v = *p; printf(\"%d\\n\", v); return 0; }\n", "addr_deref")]
    [DataRow("#include <stdio.h>\nstruct P { int x; int y; };\nint main() { struct P p; p.x = 3; p.y = 4; printf(\"%d\\n\", p.x + p.y); return 0; }\n", "struct_fields")]
    [DataRow("#include <stdio.h>\nint main() { int s = sizeof(int); printf(\"%d\\n\", s); return 0; }\n", "sizeof_int")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; long y = (long)x; int z = (int)y; printf(\"%d\\n\", z); return 0; }\n", "casts")]
    [DataRow("#include <stdio.h>\nint main() { int x = 2; switch (x) { case 1: printf(\"a\"); break; case 2: printf(\"b\"); break; default: printf(\"c\"); break; } return 0; }\n", "switch_default")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; do { x = x - 1; } while (x > 0); printf(\"%d\\n\", x); return 0; }\n", "do_while")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int y = -x; int z = !x; int w = ~x; printf(\"%d\\n\", y); return 0; }\n", "unary_ops")]
    [DataRow("#include <stdio.h>\nint main() { int a = 10; int b = 3; printf(\"%d %d\\n\", a, b); return 0; }\n", "printf_two_args")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; if (x > 0 && x < 10) printf(\"in\"); else printf(\"out\"); return 0; }\n", "logical_and")]
    [DataRow("#include <stdio.h>\nint main() { int x = 50; if (x < 0 || x > 100) printf(\"out\"); else printf(\"in\"); return 0; }\n", "logical_or")]
    [DataRow("#include <stdio.h>\nint fact(int n) { if (n <= 1) return 1; return n * fact(n - 1); }\nint main() { printf(\"%d\\n\", fact(5)); return 0; }\n", "recursion")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; x += 3; x -= 1; x *= 2; x /= 2; x %= 5; printf(\"%d\\n\", x); return 0; }\n", "compound_arith")]
    [DataRow("#include <stdio.h>\nint main() { int x = 0xFF; x &= 0x0F; x |= 0x10; x ^= 0xFF; printf(\"%d\\n\", x); return 0; }\n", "compound_bitwise")]
    [DataRow("#include <stdio.h>\nint main() { int x = 1; x <<= 4; x >>= 1; printf(\"%d\\n\", x); return 0; }\n", "compound_shift")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int y = x > 0 ? 1 : -1; printf(\"%d\\n\", y); return 0; }\n", "ternary")]
    [DataRow("#include <stdio.h>\nint main() { int a[5]; for (int i = 0; i < 5; i++) a[i] = i + 1; int s = 0; for (int i = 0; i < 5; i++) s += a[i]; printf(\"%d\\n\", s); return 0; }\n", "array_sum")]
    public async Task Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.C, source, $"{label}.c");
        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
