namespace Stack86.Logic.Test.Languages.CSharp;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// C# programs targeting under-covered CSharpIrGenerator paths:
/// property accessors, object creation, identifier resolution,
/// formatted print statements.
/// </summary>
[TestClass]
public sealed class CSharpAdvancedCompileTests
{
    [TestMethod]
    [DataRow("class P { public int X { get; set; } public int Y { get; set; } public static void Main() { var p = new P { X = 3, Y = 4 }; System.Console.WriteLine(p.X + p.Y); } }", "auto_props_init")]
    [DataRow("class P { public int X { get; set; } public static void Main() { var p = new P(); p.X = 42; System.Console.WriteLine(p.X); } }", "auto_prop_assign")]
    [DataRow("class P { private int x; public int X { get { return x; } set { x = value; } } public static void Main() { var p = new P(); p.X = 7; System.Console.WriteLine(p.X); } }", "explicit_props")]
    [DataRow("class P { public static int Sq(int v) => v * v; public static void Main() { System.Console.WriteLine(Sq(6)); } }", "expression_body")]
    [DataRow("class P { public int A; public int B; public static void Main() { var p = new P(); p.A = 1; p.B = 2; System.Console.WriteLine(p.A + p.B); } }", "public_fields")]
    [DataRow("class P { public static void Main() { int x = 5; System.Console.Write(\"x=\"); System.Console.WriteLine(x); } }", "write_then_writeline")]
    [DataRow("class P { public static void Main() { System.Console.WriteLine(1); System.Console.WriteLine(2); System.Console.WriteLine(3); } }", "many_writelines")]
    [DataRow("class P { public static int Add(int a, int b) { return a + b; } public static int Sub(int a, int b) { return a - b; } public static void Main() { System.Console.WriteLine(Add(10, Sub(20, 5))); } }", "static_method_chain")]
    [DataRow("class P { public static void Main() { int sum = 0; for (int i = 1; i <= 10; i++) sum += i; System.Console.WriteLine(sum); } }", "for_sum")]
    [DataRow("class P { public static void Main() { int x = 7; if (x > 0) { if (x > 5) System.Console.WriteLine(\"big\"); else System.Console.WriteLine(\"mid\"); } else System.Console.WriteLine(\"neg\"); } }", "nested_if")]
    [DataRow("class P { public static void Main() { int x = 100; while (x > 0) { System.Console.WriteLine(x); x -= 25; } } }", "while_dec")]
    [DataRow("class P { public static int Fact(int n) { if (n <= 1) return 1; return n * Fact(n - 1); } public static void Main() { System.Console.WriteLine(Fact(6)); } }", "recursion")]
    [DataRow("class P { public int Val; public P(int v) { this.Val = v; } public static void Main() { var p = new P(99); System.Console.WriteLine(p.Val); } }", "ctor_with_arg")]
    [DataRow("class P { public static void Main() { string s = \"hi\"; System.Console.WriteLine(s); System.Console.WriteLine(s); } }", "string_var_print")]
    [DataRow("class P { public static void Main() { bool a = true; bool b = false; System.Console.WriteLine(a && !b); System.Console.WriteLine(a || b); } }", "bool_ops")]
    public async Task CSharpProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.CSharp, source, $"{label}.cs");

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
