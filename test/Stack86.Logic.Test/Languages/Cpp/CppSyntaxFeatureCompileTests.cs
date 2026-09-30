namespace Stack86.Logic.Test.Languages.Cpp;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// C++ programs targeting under-covered CppParser/CppIrGenerator paths:
/// do-while, sizeof, unary ops, types, enums, variable forms.
/// </summary>
[TestClass]
public sealed class CppSyntaxFeatureCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nint main() { int x = 0; do { x++; } while (x < 5); printf(\"%d\", x); return 0; }", "do_while")]
    [DataRow("#include <stdio.h>\nint main() { int n = sizeof(int); int m = sizeof(char); printf(\"%d %d\", n, m); return 0; }", "sizeof_types")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int n = sizeof(x); printf(\"%d\", n); return 0; }", "sizeof_var")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int y = -x; int z = ~x; int w = !x; printf(\"%d\", y); return 0; }", "unary_ops")]
    [DataRow("#include <stdio.h>\nint main() { int x = 0; x++; ++x; x--; --x; printf(\"%d\", x); return 0; }", "inc_dec")]
    [DataRow("#include <stdio.h>\nint main() { int x = 1 << 4; int y = x >> 2; int z = x & 0xFF; int w = x | 0x10; int t = x ^ 0xFF; printf(\"%d\", z); return 0; }", "bitwise")]
    [DataRow("#include <stdio.h>\nint main() { int x = 10; x += 5; x -= 2; x *= 3; x /= 2; x %= 4; printf(\"%d\", x); return 0; }", "compound_assign")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; switch (x) { case 1: printf(\"a\"); break; case 5: printf(\"b\"); break; default: printf(\"c\"); } return 0; }", "switch")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; const int K = 10; int y = x + K; printf(\"%d\", y); return 0; }", "const")]
    [DataRow("#include <stdio.h>\nint sq(int x) { return x * x; }\nint cube(int x) { return x * x * x; }\nint main() { printf(\"%d\", sq(3) + cube(2)); return 0; }", "multi_function")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; if (x > 0 && x < 10) printf(\"in range\"); if (x < 0 || x > 100) printf(\"out\"); return 0; }", "logical")]
    public async Task CppProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.Cpp, source, $"{label}.cpp");

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
