namespace Stack86.Logic.Test.Compilation;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;

/// <summary>
/// Compiles every sample program shipped in the SPA <c>compile-toolbar/samples</c>
/// directory through the corresponding language pipeline. Each sample is exercised
/// independently to give broad coverage of every language frontend.
/// </summary>
[TestClass]
public sealed class SamplesCompileEndToEndTests
{
    private static readonly Dictionary<string, SupportedLanguage> LanguageMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["js"] = SupportedLanguage.JavaScript,
        ["ts"] = SupportedLanguage.TypeScript,
        ["cpp"] = SupportedLanguage.Cpp,
        ["csharp"] = SupportedLanguage.CSharp,
    };

    public static IEnumerable<object[]> SampleFiles()
    {
        var samplesRoot = ResolveSamplesRoot();
        if (samplesRoot is null)
        {
            yield break;
        }

        foreach (var langDir in Directory.EnumerateDirectories(samplesRoot))
        {
            var langName = Path.GetFileName(langDir);
            if (!LanguageMap.TryGetValue(langName, out var lang))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(langDir))
            {
                yield return [lang.Value, Path.GetFileName(file), file];
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(SampleFiles))]
    public async Task Sample_CompilesWithoutThrowing(string langValue, string fileName, string fullPath)
    {
        Assert.IsTrue(SupportedLanguage.TryFromValue(langValue, out var lang));
        var source = await File.ReadAllTextAsync(fullPath);

        var result = await CompileFixture.CompileLanguage(lang, source, fileName);

        // For samples that fully succeed, also exercise the assembly assertion path.
        if (result.IsSuccess && result.Value.Errors.Count == 0)
        {
            Assert.IsNotNull(result.Value.Assembly);
        }
    }

    private static string? ResolveSamplesRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src",
                "Stack86.Web",
                "ClientApp",
                "src",
                "app",
                "features",
                "compiler",
                "compile-toolbar",
                "samples");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
