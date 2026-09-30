namespace Stack86.Logic.Test.Languages.Cpp;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// C++ programs targeting under-covered CppParser paths:
/// enum declarations, type variants, function/variable forms, ternary, primary expressions.
/// </summary>
[TestClass]
public sealed class CppParserFeatureCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int y = (x > 0) ? x : -x; printf(\"%d\", y); return 0; }", "ternary")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int y = (x > 0) ? ((x > 10) ? 100 : 50) : 0; printf(\"%d\", y); return 0; }", "nested_ternary")]
    [DataRow("#include <stdio.h>\nint global_x = 100;\nint main() { printf(\"%d\", global_x); return 0; }", "global_var")]
    [DataRow("#include <stdio.h>\nstatic int helper(int x) { return x + 1; }\nint main() { printf(\"%d\", helper(5)); return 0; }", "static_function")]
    [DataRow("#include <stdio.h>\nint main() { int* p = 0; if (p == 0) printf(\"null\"); return 0; }", "null_pointer")]
    [DataRow("#include <stdio.h>\nint sum(int a, int b, int c) { return a + b + c; }\nint main() { int r = sum(1, 2, 3); printf(\"%d\", r); return 0; }", "function_three_args")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int y = 10; int z = x < y ? y - x : x - y; printf(\"%d\", z); return 0; }", "ternary_with_arith")]
    [DataRow("#include <stdio.h>\nint factorial(int n) { if (n <= 1) return 1; return n * factorial(n - 1); }\nint main() { printf(\"%d\", factorial(6)); return 0; }", "recursion_return")]
    [DataRow("#include <stdio.h>\nint main() { int n = 0; for (int i = 1; i <= 10; i++) n = n + i; printf(\"%d\", n); return 0; }", "for_sum")]
    public async Task CppProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.Cpp, source, $"{label}.cpp");

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
