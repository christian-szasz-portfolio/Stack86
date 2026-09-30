namespace Stack86.Logic.Test.Languages.CSharp;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Round-five C# compile tests targeting <c>CSharpIrGenerator</c> gaps.
/// </summary>
[TestClass]
public sealed class CSharpRoundFiveCompileTests
{
    [TestMethod]
    [DataRow("class P { static void Main() { for (int i = 0; i < 5; i++) System.Console.WriteLine(i); } }", "for_loop")]
    [DataRow("class P { static void Main() { int i = 0; while (i < 4) { System.Console.WriteLine(i); i++; } } }", "while_loop")]
    [DataRow("class P { static void Main() { int i = 0; do { System.Console.WriteLine(i); i++; } while (i < 3); } }", "do_while")]
    [DataRow("class P { static int Add(int a, int b) { return a + b; } static void Main() { System.Console.WriteLine(Add(2, 3)); } }", "static_method_call")]
    [DataRow("class P { static int Sq(int x) => x * x; static void Main() { System.Console.WriteLine(Sq(7)); } }", "expression_bodied")]
    [DataRow("class P { static void Main() { int x = 5; int y = (x > 0) ? 1 : -1; System.Console.WriteLine(y); } }", "ternary")]
    [DataRow("class P { static void Main() { int a = 0xFF; int b = a & 0x0F; int c = a | 0x10; int d = a ^ 0x55; System.Console.WriteLine(b); System.Console.WriteLine(c); System.Console.WriteLine(d); } }", "bitwise")]
    [DataRow("class P { static void Main() { int a = 1; a += 5; a -= 1; a *= 3; a /= 2; a %= 4; System.Console.WriteLine(a); } }", "compound")]
    [DataRow("class P { static void Main() { int x = 3; switch (x) { case 1: System.Console.WriteLine(\"a\"); break; case 3: System.Console.WriteLine(\"c\"); break; default: System.Console.WriteLine(\"d\"); break; } } }", "switch")]
    [DataRow("class P { class Box { public int V; } static void Main() { Box b = new Box(); b.V = 42; System.Console.WriteLine(b.V); } }", "class_with_field")]
    [DataRow("class P { class Box { public int V { get; set; } } static void Main() { Box b = new Box(); b.V = 7; System.Console.WriteLine(b.V); } }", "auto_property")]
    [DataRow("class P { static int Fact(int n) { if (n <= 1) return 1; return n * Fact(n - 1); } static void Main() { for (int i = 1; i <= 5; i++) System.Console.WriteLine(Fact(i)); } }", "recursion_in_loop")]
    [DataRow("class P { static void Main() { int sum = 0; for (int i = 0; i < 5; i++) { if (i == 2) continue; if (i == 4) break; sum = sum + i; } System.Console.WriteLine(sum); } }", "break_continue")]
    [DataRow("class P { class A { public int F() { return 1; } } class B : A { public new int F() { return 2; } } static void Main() { B b = new B(); System.Console.WriteLine(b.F()); } }", "inheritance_new")]
    [DataRow("class P { static void Main() { int a = 5; int b = -a; int c = +a; bool d = !(a == 0); System.Console.WriteLine(b); System.Console.WriteLine(c); System.Console.WriteLine(d); } }", "unary_ops")]
    public async Task Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.CSharp, source, $"{label}.cs");
        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
