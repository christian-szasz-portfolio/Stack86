namespace Stack86.Logic.Test.Languages.Cpp;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// C++ programs targeting CppParser advanced paths:
/// type variants, function/variable disambiguation, primary expressions.
/// </summary>
[TestClass]
public sealed class CppParserAdvancedCompileTests
{
    [TestMethod]
    [DataRow("#include <cstdio>\nint main() { const int x = 5; printf(\"%d\", x); return 0; }", "const_var")]
    [DataRow("#include <cstdio>\nint main() { static int x = 7; printf(\"%d\", x); return 0; }", "static_var")]
    [DataRow("#include <cstdio>\nint main() { int x = 0; for (int i = 0; i < 3; ++i) { x += i; } printf(\"%d\", x); return 0; }", "pre_increment")]
    [DataRow("#include <cstdio>\nint main() { int x = 0; for (int i = 0; i < 3; i++) { x += i; } printf(\"%d\", x); return 0; }", "post_increment")]
    [DataRow("#include <cstdio>\nint sq(int x) { return x * x; }\nint cube(int x) { return x * x * x; }\nint main() { printf(\"%d %d\", sq(4), cube(3)); return 0; }", "two_funcs")]
    [DataRow("#include <cstdio>\nvoid greet() { printf(\"hi\"); }\nint main() { greet(); return 0; }", "void_func")]
    [DataRow("#include <cstdio>\nint main() { int x = 5; int *p = &x; printf(\"%d\", *p); return 0; }", "pointer_basic")]
    [DataRow("#include <cstdio>\nint main() { int a = 10; int &r = a; r = 20; printf(\"%d\", a); return 0; }", "reference")]
    [DataRow("#include <cstdio>\nint main() { for (int i = 0; i < 5; i++) { if (i == 3) break; printf(\"%d\", i); } return 0; }", "for_with_break")]
    [DataRow("#include <cstdio>\nint main() { int i = 0; while (i < 5) { if (i == 2) { i++; continue; } printf(\"%d\", i); i++; } return 0; }", "while_with_continue")]
    [DataRow("#include <cstdio>\nint main() { int x = 5; switch (x) { case 1: printf(\"a\"); break; case 5: printf(\"b\"); break; default: printf(\"c\"); break; } return 0; }", "switch_default")]
    [DataRow("#include <cstdio>\nint main() { int a = 1; int b = 2; int c = 3; printf(\"%d\", a + b + c); return 0; }", "comma_decl")]
    [DataRow("#include <cstdio>\nint main() { int x = 5; int y = ++x; printf(\"%d %d\", x, y); return 0; }", "pre_inc_value")]
    [DataRow("#include <cstdio>\nint main() { int x = 5; int y = x++; printf(\"%d %d\", x, y); return 0; }", "post_inc_value")]
    public async Task CppProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.Cpp, source, $"{label}.cpp");

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
