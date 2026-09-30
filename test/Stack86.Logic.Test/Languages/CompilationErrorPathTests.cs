namespace Stack86.Logic.Test.Languages;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Tests that intentionally invalid programs across all supported languages
/// are rejected by the pipeline. Exercises parser/validator error-handling branches
/// that successful programs do not reach.
/// </summary>
[TestClass]
public sealed class CompilationErrorPathTests
{
    [TestMethod]
    [DataRow("c", "int main() { int x = ; return 0; }", "c_missing_expr")]
    [DataRow("c", "int main( { return 0; }", "c_unclosed_paren")]
    [DataRow("c", "int main() { if (x == 1 { } return 0; }", "c_missing_close_paren")]
    [DataRow("c", "int main() { return 0", "c_missing_semicolon_brace")]
    [DataRow("c", "int main() { int = 5; return 0; }", "c_missing_var_name")]
    [DataRow("cpp", "int main() { class { int x; }; return 0; }", "cpp_anon_class_no_body")]
    [DataRow("cpp", "int main() { struct A { int x; A( { } }; return 0; }", "cpp_ctor_unclosed")]
    [DataRow("csharp", "class P { static void Main() { int = 5; } }", "cs_var_no_name")]
    [DataRow("csharp", "class { static void Main() { } }", "cs_class_no_name")]
    public async Task Fails(string langValue, string source, string label)
    {
        Assert.IsTrue(SupportedLanguage.TryFromValue(langValue, out var lang));
        var ext = ExtensionFor(lang);
        var result = await CompileFixture.CompileLanguage(lang, source, $"{label}{ext}");
        Assert.IsTrue(result.IsFailure, $"{label} should fail to compile but succeeded.");
    }

    private static string ExtensionFor(SupportedLanguage lang) => lang.Value switch
    {
        "c" => ".c",
        "cpp" => ".cpp",
        "csharp" => ".cs",
        "javascript" => ".js",
        "typescript" => ".ts",
        _ => ".txt",
    };
}
