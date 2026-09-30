namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Test.Compilation;

/// <summary>
/// Targeted C compilation tests for stdlib functions and IR-generator features
/// missed by the SPA samples: scanf, puts, ternary, exit, sizeof, abs, syscall macros.
/// </summary>
[TestClass]
public sealed class CFeatureCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nint main() { int x; scanf(\"%d\", &x); printf(\"%d\", x); return 0; }", "scanf_int")]
    [DataRow("#include <stdio.h>\nint main() { char c; scanf(\"%c\", &c); printf(\"%c\", c); return 0; }", "scanf_char")]
    [DataRow("#include <stdio.h>\nint main() { puts(\"hello\"); return 0; }", "puts")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int y = (x > 0) ? 1 : -1; return y; }", "ternary")]
    [DataRow("#include <stdlib.h>\nint main() { exit(0); return 0; }", "exit")]
    [DataRow("int main() { int n = sizeof(int); return n; }", "sizeof_int")]
    [DataRow("int main() { int a[10]; int n = sizeof(a); return n; }", "sizeof_array")]
    [DataRow("#include <stdlib.h>\nint main() { int x = abs(-5); return x; }", "abs")]
    [DataRow("#include <stdio.h>\nchar buf[20];\nint main() { gets(buf); return 0; }", "gets")]
    [DataRow("#include <stdio.h>\nint main() { putchar('A'); return 0; }", "putchar")]
    [DataRow("#include <stdio.h>\nint main() { int c = getchar(); return c; }", "getchar")]
    [DataRow("int main() { int x = 5; int y; y = x > 0 ? x : -x; return y; }", "ternary_assign")]
    public async Task CProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileC(source);

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
