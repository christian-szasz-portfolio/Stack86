namespace Stack86.Logic.Test.Languages.Cpp;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Round-five C++ compile tests targeting remaining gaps in
/// <c>CppParser</c> and <c>CppToCEmitter</c> (classes, methods, references,
/// access specifiers, scope resolution, member init).
/// </summary>
[TestClass]
public sealed class CppRoundFiveCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nstruct Pt { int x; int y; void set(int a, int b) { x = a; y = b; } int sum() { return x + y; } };\nint main() { Pt p; p.set(3, 4); printf(\"%d\", p.sum()); return 0; }", "struct_methods")]
    [DataRow("#include <stdio.h>\nclass Box { public: int v; void put(int x) { v = x; } int get() { return v; } };\nint main() { Box b; b.put(7); printf(\"%d\", b.get()); return 0; }", "class_public_method")]
    [DataRow("#include <stdio.h>\nclass C { private: int x; public: void set(int v) { x = v; } int get() { return x; } };\nint main() { C c; c.set(11); printf(\"%d\", c.get()); return 0; }", "class_private_field_public_methods")]
    [DataRow("#include <stdio.h>\nstruct A { int v; A(int x) { v = x; } };\nint main() { A a(9); printf(\"%d\", a.v); return 0; }", "struct_constructor")]
    [DataRow("#include <stdio.h>\nvoid inc(int &x) { x = x + 1; }\nint main() { int n = 5; inc(n); inc(n); printf(\"%d\", n); return 0; }", "reference_param_inc")]
    [DataRow("#include <stdio.h>\nvoid swap(int &a, int &b) { int t = a; a = b; b = t; }\nint main() { int x = 1; int y = 2; swap(x, y); printf(\"%d %d\", x, y); return 0; }", "reference_param_swap")]
    [DataRow("#include <stdio.h>\nnamespace m { int square(int x) { return x * x; } }\nint main() { printf(\"%d\", m::square(6)); return 0; }", "namespace_function")]
    [DataRow("#include <stdio.h>\nstruct A { int v; }; struct B { int w; };\nint main() { A a; B b; a.v = 3; b.w = 4; printf(\"%d\", a.v + b.w); return 0; }", "two_structs")]
    [DataRow("#include <stdio.h>\nstruct P { int x; int y; }; int dist2(struct P p) { return p.x * p.x + p.y * p.y; }\nint main() { struct P p; p.x = 3; p.y = 4; printf(\"%d\", dist2(p)); return 0; }", "struct_value_param")]
    [DataRow("#include <stdio.h>\nint main() { int a = 10; int *p = &a; *p = 99; printf(\"%d\", a); return 0; }", "pointer_deref_assign")]
    [DataRow("#include <stdio.h>\nint twice(int *p) { return *p * 2; }\nint main() { int a = 21; printf(\"%d\", twice(&a)); return 0; }", "pointer_arg_deref")]
    public async Task Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.Cpp, source, $"{label}.cpp");
        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
