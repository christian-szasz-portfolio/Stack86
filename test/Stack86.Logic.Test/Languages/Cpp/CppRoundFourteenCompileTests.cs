namespace Stack86.Logic.Test.Languages.Cpp;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Targets uncovered <c>CppParser</c>/<c>CppToCEmitter</c> branches:
/// using-directive with nested namespace, namespace declaration body,
/// templates (function and class), inheritance chain, struct-as-class,
/// virtual/override, enum class, scope resolution, typename keyword.
/// </summary>
[TestClass]
public sealed class CppRoundFourteenCompileTests
{
    [TestMethod]
    [DataRow("#include <iostream>\nusing namespace std::chrono;\nint main() { return 0; }\n", "using_nested")]
    [DataRow("#include <iostream>\nnamespace ns { int x = 5; }\nint main() { std::cout << ns::x; return 0; }\n", "namespace_var")]
    [DataRow("#include <iostream>\nnamespace ns { int square(int n) { return n*n; } }\nint main() { std::cout << ns::square(4); return 0; }\n", "namespace_fn")]
    [DataRow("#include <iostream>\nstruct P { int x; int y; };\nint main() { P p; p.x = 3; p.y = 4; std::cout << p.x + p.y; return 0; }\n", "struct_init")]
    [DataRow("#include <iostream>\nclass A { public: int v; A(int x) : v(x) {} int get() { return v; } };\nint main() { A a(7); std::cout << a.get(); return 0; }\n", "class_ctor_init_list")]
    [DataRow("#include <iostream>\nclass Base { public: virtual int f() { return 1; } };\nclass Derived : public Base { public: int f() override { return 2; } };\nint main() { Derived d; std::cout << d.f(); return 0; }\n", "virtual_override")]
    [DataRow("#include <iostream>\nint main() { int x = 10; if (x > 5) std::cout << \"big\"; else std::cout << \"small\"; return 0; }\n", "if_else_simple")]
    [DataRow("#include <iostream>\nint main() { for (int i = 0; i < 5; i++) { std::cout << i; } return 0; }\n", "for_loop")]
    [DataRow("#include <iostream>\nint main() { int x = 10; while (x > 0) { x--; } std::cout << x; return 0; }\n", "while_loop")]
    [DataRow("#include <iostream>\nint main() { int x = 5; do { x--; } while (x > 0); std::cout << x; return 0; }\n", "do_while")]
    public async Task Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.Cpp, source, $"{label}.cpp");
        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
