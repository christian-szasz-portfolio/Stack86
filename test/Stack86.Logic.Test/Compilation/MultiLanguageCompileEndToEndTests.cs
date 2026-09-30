namespace Stack86.Logic.Test.Compilation;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;

/// <summary>
/// End-to-end compilation tests for each non-C language. Each test runs the full
/// per-language stage chain to provide broad coverage of the corresponding lexer,
/// parser, IR generator and capability validator.
/// </summary>
[TestClass]
public sealed class MultiLanguageCompileEndToEndTests
{
    [TestMethod]
    public async Task JavaScript_HelloWorld_Compiles()
    {
        var src = "console.log(\"hi\");";
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.JavaScript, src, "main.js");
        AssertSuccessOrSoft(result);
    }

    [TestMethod]
    public async Task TypeScript_HelloWorld_Compiles()
    {
        var src = "console.log(\"hi\");";
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.TypeScript, src, "main.ts");
        AssertSuccessOrSoft(result);
    }

    [TestMethod]
    public async Task Cpp_BasicProgram_Compiles()
    {
        var src = """
            #include <stdio.h>
            int main() { printf("hi"); return 0; }
            """;
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.Cpp, src, "main.cpp");
        AssertSuccessOrSoft(result);
    }

    [TestMethod]
    public async Task CSharp_HelloWorld_Compiles()
    {
        var src = """
            using System;
            class Program {
                static void Main() { Console.WriteLine("hi"); }
            }
            """;
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.CSharp, src, "Program.cs");
        AssertSuccessOrSoft(result);
    }

    /// <summary>
    /// Per-language frontends may legitimately surface diagnostics for unsupported features.
    /// We assert the pipeline ran to completion (no fatal stage failure) without throwing.
    /// </summary>
    private static void AssertSuccessOrSoft(CompileResult result)
    {
        Assert.IsTrue(
            result.IsSuccess,
            $"pipeline failed unexpectedly: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
