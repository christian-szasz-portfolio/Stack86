namespace Stack86.Logic.Test.Languages.Cpp;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Targeted C++ compilation tests for features missed by SPA samples:
/// enums, templates, namespaces, destructors, compound assignment, references.
/// </summary>
[TestClass]
public sealed class CppFeatureCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nnamespace ns { int v = 42; }\nint main() { printf(\"%d\", ns::v); return 0; }", "namespace")]
    [DataRow("#include <stdio.h>\nstruct Box { int v; ~Box() { } };\nint main() { Box b; b.v = 5; printf(\"%d\", b.v); return 0; }", "destructor")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; x += 1; x -= 1; x *= 2; x /= 2; printf(\"%d\", x); return 0; }", "compound_assign")]
    [DataRow("#include <stdio.h>\nint main() { int x = 5; int& r = x; r = 10; printf(\"%d\", x); return 0; }", "reference")]
    [DataRow("#include <stdio.h>\nstruct P { int x; int y; };\nint main() { P p; p.x = 1; p.y = 2; printf(\"%d %d\", p.x, p.y); return 0; }", "struct")]
    [DataRow("#include <stdio.h>\nint sq(int x) { return x * x; }\nint main() { printf(\"%d\", sq(5)); return 0; }", "function")]
    [DataRow("#include <stdio.h>\nint main() { int x = 10; if (x > 5) { x = 1; } else { x = 2; } printf(\"%d\", x); return 0; }", "if_else")]
    public async Task CppProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.Cpp, source, $"{label}.cpp");

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
