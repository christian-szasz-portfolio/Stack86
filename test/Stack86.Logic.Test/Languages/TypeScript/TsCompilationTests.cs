namespace Stack86.Logic.Test.Languages.TypeScript;

using System.Linq;
using System.Threading.Tasks;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// End-to-end coverage for the TypeScript stage chain. These tests assert that the
/// project source actually flows into the transpiler and capability validator: a single
/// TypeScript file must produce real 8086 output (not an empty translation unit), and
/// unsupported keywords must be flagged by the capability validator (which only runs if it
/// receives the real source).
/// </summary>
[TestClass]
public sealed class TsCompilationTests
{
    [TestMethod]
    public async Task SingleFile_ProducesAssemblyContainingProgramOutput()
    {
        // console.log lowers to a print syscall (INT 21h). If the source never reached the
        // transpiler/frontend, the emitted assembly would carry no print instruction.
        var src = "let x = 5;\nconsole.log(x);\n";

        var result = await CompileFixture.CompileLanguage(SupportedLanguage.TypeScript, src, "main.ts");

        Assert.IsTrue(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);
        Assert.IsNotNull(result.Value.Assembly);
        StringAssert.Contains(result.Value.Assembly!, "INT 21h");
    }

    [TestMethod]
    public async Task UnsupportedKeyword_IsFlaggedByCapabilityValidator()
    {
        // parseFloat is rejected by TsCapabilityValidator. The validator only sees the source
        // if the preprocess stage populated it, so this exercises the full pre-transpile path.
        var src = "let x = parseFloat(\"1.5\");\nconsole.log(x);\n";

        var result = await CompileFixture.CompileLanguage(SupportedLanguage.TypeScript, src, "main.ts");

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Value.Assembly);
        Assert.IsTrue(result.Value.Errors.Any(e => e.Message.Contains("parseFloat")));
    }
}
