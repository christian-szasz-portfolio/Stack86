namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Compilation;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Verifies multi-file C projects: the entry point defaults to <c>main.c</c> and quoted
/// <c>#include</c> directives pull in helper headers/translation units from the project file set.
/// </summary>
[TestClass]
public sealed class CMultiFileCompileTests
{
    [TestMethod]
    public async Task MainFile_IncludesQuotedHeader_Compiles()
    {
        var files = new Dictionary<string, string>
        {
            ["math.h"] = "int square(int n) { return n * n; }\n",
            ["main.c"] = "#include <stdio.h>\n#include \"math.h\"\nint main() { printf(\"%d\", square(6)); return 0; }\n",
        };

        var result = await CompileFixture.CompileFiles(SupportedLanguage.C, files);

        Assert.IsTrue(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);
        Assert.IsTrue(result.Value.Assembly!.Contains(".CODE", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task MainFile_IncludesObjectLikeMacro_Compiles()
    {
        var files = new Dictionary<string, string>
        {
            ["config.h"] = "#define LIMIT 10\n",
            ["main.c"] = "#include <stdio.h>\n#include \"config.h\"\nint main() { int x = LIMIT; printf(\"%d\", x); return 0; }\n",
        };

        var result = await CompileFixture.CompileFiles(SupportedLanguage.C, files);

        Assert.IsTrue(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);
    }

    [TestMethod]
    public async Task MissingIncludeFile_Fails()
    {
        var files = new Dictionary<string, string>
        {
            ["main.c"] = "#include \"missing.h\"\nint main() { return 0; }\n",
        };

        var result = await CompileFixture.CompileFiles(SupportedLanguage.C, files);

        Assert.IsTrue(result.IsFailure);
    }
}
