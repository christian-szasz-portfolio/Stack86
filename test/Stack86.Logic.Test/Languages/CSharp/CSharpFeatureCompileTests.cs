namespace Stack86.Logic.Test.Languages.CSharp;

using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// C# programs targeting under-covered Roslyn-driven IR generator paths:
/// short-circuit logical, conditional, prefix unary, pre-inc/dec, property accessors,
/// object creation, compound assignment, char/string/double literals.
/// </summary>
[TestClass]
public sealed class CSharpFeatureCompileTests
{
    [TestMethod]
    [DataRow("class Program { static void Main() { int x = 5; int y = -x; int z = ~x; bool b = !true; System.Console.Write(y); } }", "prefix_unary")]
    [DataRow("class Program { static void Main() { int x = 0; ++x; --x; System.Console.Write(x); } }", "pre_inc_dec")]
    [DataRow("class Program { static void Main() { int x = 0; x++; x--; System.Console.Write(x); } }", "post_inc_dec")]
    [DataRow("class Program { static void Main() { bool a = true; bool b = false; if (a && b) System.Console.Write(1); if (a || b) System.Console.Write(2); } }", "logical_short_circuit")]
    [DataRow("class Program { static void Main() { int x = 5; int y = (x > 0) ? 1 : -1; System.Console.Write(y); } }", "conditional")]
    [DataRow("class Program { static void Main() { char c = 'A'; System.Console.Write(c); } }", "char_literal")]
    [DataRow("class Program { static void Main() { string s = \"hello\"; System.Console.Write(s); } }", "string_literal")]
    [DataRow("class Program { static void Main() { int x = 10; x += 5; x -= 2; x *= 3; x /= 2; x %= 4; System.Console.Write(x); } }", "compound_assign")]
    [DataRow("class Program { static int Add(int a, int b) { return a + b; } static void Main() { System.Console.Write(Add(2, 3)); } }", "static_method")]
    [DataRow("class Program { class Box { public int V; } static void Main() { Box b = new Box(); b.V = 42; System.Console.Write(b.V); } }", "object_creation_field")]
    [DataRow("class Program { class Box { public int V { get; set; } } static void Main() { Box b = new Box(); b.V = 42; System.Console.Write(b.V); } }", "auto_property")]
    [DataRow("class Program { static void Main() { int i = 0; while (i < 5) { i++; } System.Console.Write(i); } }", "while_loop")]
    [DataRow("class Program { static void Main() { for (int i = 0; i < 3; i++) System.Console.Write(i); } }", "for_loop")]
    [DataRow("class Program { static void Main() { int x = 5; if (x > 0) { System.Console.Write(1); } else { System.Console.Write(2); } } }", "if_else")]
    [DataRow("class Program { static void Main() { int x = 1 << 4; int y = x >> 2; int z = x & 0xF; int w = x | 0x10; int t = x ^ 0xFF; System.Console.Write(z); } }", "bitwise")]
    public async Task CSharpProgram_Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.CSharp, source, "Program.cs");

        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
