namespace Stack86.Logic.Test.Languages.Cpp;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Round-ten C++ compile tests targeting remaining gaps in
/// <c>CppParser</c> and <c>CppToCEmitter</c>.
/// </summary>
[TestClass]
public sealed class CppRoundTenCompileTests
{
    [TestMethod]
    [DataRow("#include <stdio.h>\nclass A { public: int v; A() { v = 1; } }; class B : public A { public: int w; B() { w = 2; } };\nint main() { B b; printf(\"%d\", b.v + b.w); return 0; }", "class_inheritance_public")]
    [DataRow("#include <stdio.h>\nclass C { public: int x; C(int v) : x(v) {} };\nint main() { C c(15); printf(\"%d\", c.x); return 0; }", "ctor_init_list")]
    [DataRow("#include <stdio.h>\nstruct S { int a; int b; S(int x, int y) : a(x), b(y) {} };\nint main() { S s(3, 4); printf(\"%d\", s.a + s.b); return 0; }", "ctor_init_list_two")]
    [DataRow("#include <stdio.h>\nclass C { public: int v; C() { v = 5; } ~C() { } };\nint main() { C c; printf(\"%d\", c.v); return 0; }", "class_destructor")]
    [DataRow("#include <stdio.h>\nstruct A { int v; static int total; }; int A::total = 0;\nint main() { A a; a.v = 7; A::total = 99; printf(\"%d\", a.v + A::total); return 0; }", "static_member_var")]
    [DataRow("#include <stdio.h>\nstruct C { static int counter() { return 42; } };\nint main() { printf(\"%d\", C::counter()); return 0; }", "static_member_method")]
    [DataRow("#include <stdio.h>\nclass C { public: int v; C(int x) { v = x; } C(const C &o) { v = o.v + 100; } };\nint main() { C a(5); C b(a); printf(\"%d\", b.v); return 0; }", "copy_constructor")]
    [DataRow("#include <stdio.h>\nint main() { int a = 5; int &r = a; r = 99; printf(\"%d\", a); return 0; }", "reference_local")]
    [DataRow("#include <stdio.h>\nstruct P { int x; int y; }; int main() { P *p = new P(); p->x = 1; p->y = 2; printf(\"%d\", p->x + p->y); delete p; return 0; }", "new_delete")]
    [DataRow("#include <stdio.h>\nnamespace a { namespace b { int v() { return 7; } } }\nint main() { printf(\"%d\", a::b::v()); return 0; }", "nested_namespace")]
    [DataRow("#include <stdio.h>\nnamespace n { int x = 5; int get() { return x; } }\nusing namespace n;\nint main() { printf(\"%d\", get()); return 0; }", "using_namespace")]
    [DataRow("#include <stdio.h>\nint main() { for (int i = 0; i < 3; i++) { for (int j = 0; j < 3; j++) { if (j == 1) continue; if (i == 2) break; } } printf(\"%d\", 1); return 0; }", "nested_for_break_continue")]
    public async Task Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.Cpp, source, $"{label}.cpp");
        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
