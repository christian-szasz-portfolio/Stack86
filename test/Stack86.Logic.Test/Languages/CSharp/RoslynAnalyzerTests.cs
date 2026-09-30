namespace Stack86.Logic.Test.Languages.CSharp;

using System.Collections.Generic;
using Stack86.Common.Exceptions;
using Stack86.Logic.Languages.CSharp;

/// <summary>
/// Direct unit tests for <see cref="RoslynAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class RoslynAnalyzerTests
{
    [TestMethod]
    public void ExtractStringLiterals_returns_unique_strings()
    {
        var files = new Dictionary<string, string>
        {
            ["main.cs"] = "class C { void M() { var a = \"hello\"; var b = \"world\"; var c = \"hello\"; } }",
        };
        var literals = RoslynAnalyzer.ExtractStringLiterals(files);
        Assert.HasCount(2, literals);
        Assert.Contains("hello", literals.Values);
        Assert.Contains("world", literals.Values);
    }

    [TestMethod]
    public void ExtractStringLiterals_skips_empty_strings()
    {
        var files = new Dictionary<string, string>
        {
            ["main.cs"] = "class C { void M() { var x = \"\"; } }",
        };
        var literals = RoslynAnalyzer.ExtractStringLiterals(files);
        Assert.IsEmpty(literals);
    }

    [TestMethod]
    public void ExtractStringLiterals_handles_multiple_files()
    {
        var files = new Dictionary<string, string>
        {
            ["a.cs"] = "class A { string s = \"alpha\"; }",
            ["b.cs"] = "class B { string s = \"beta\"; }",
        };
        var literals = RoslynAnalyzer.ExtractStringLiterals(files);
        Assert.HasCount(2, literals);
    }

    [TestMethod]
    public void Analyze_valid_program_succeeds()
    {
        var analyzer = new RoslynAnalyzer();
        var files = new Dictionary<string, string>
        {
            ["main.cs"] = "class Program { static void Main() { System.Console.WriteLine(1); } }",
        };
        var (compilation, _) = analyzer.Analyze(files);
        Assert.IsNotNull(compilation);
    }

    [TestMethod]
    public void Analyze_syntax_error_returns_failure()
    {
        var analyzer = new RoslynAnalyzer();
        var files = new Dictionary<string, string>
        {
            ["main.cs"] = "class Program { static void Main() { System.Console.WriteLine(1) }",
        };
        Assert.ThrowsExactly<CompilationFailedException>(() => analyzer.Analyze(files));
    }

    [TestMethod]
    public void Compile_valid_program_returns_pe_bytes()
    {
        var analyzer = new RoslynAnalyzer();
        var files = new Dictionary<string, string>
        {
            ["main.cs"] = "class Program { static void Main() { System.Console.WriteLine(2); } }",
        };
        var (peBytes, _) = analyzer.Compile(files);
        Assert.IsNotEmpty(peBytes);
    }

    [TestMethod]
    public void Compile_invalid_program_returns_failure()
    {
        var analyzer = new RoslynAnalyzer();
        var files = new Dictionary<string, string>
        {
            ["main.cs"] = "class Program { static void Main() { undefined_method(); } }",
        };
        Assert.ThrowsExactly<CompilationFailedException>(() => analyzer.Compile(files));
    }
}
