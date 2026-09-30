namespace Stack86.Logic.Test.Compilation.Pipeline;

using Microsoft.Extensions.DependencyInjection;
using Stack86.Logic.Compilation.Pipeline;
using Stack86.Logic.Languages;
using Stack86.Logic.Pipeline.Ir;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Unit tests for <see cref="LanguageDefaults"/>. Covers each per-language stage chain
/// and the argument-null guard via the public <see cref="LanguageDefaults.ConfigureFor"/>
/// entry point. Each chain is asserted by inspecting the pipeline's RunAsync behaviour
/// indirectly through <see cref="CompilationPipeline.For"/> + <see cref="LanguageDefaults.ConfigureFor"/>
/// (no exception means the chain composed successfully).
/// </summary>
[TestClass]
public sealed class LanguageDefaultsTests
{
    [TestMethod]
    public void ConfigureFor_NullPipeline_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => LanguageDefaults.ConfigureFor(null!, SupportedLanguage.C));
    }

    [TestMethod]
    public void ConfigureFor_NullLanguage_Throws()
    {
        var pipeline = NewPipeline(SupportedLanguage.C);

        Assert.ThrowsExactly<ArgumentNullException>(
            () => LanguageDefaults.ConfigureFor(pipeline, null!));
    }

    [TestMethod]
    [DataRow("c")]
    [DataRow("cpp")]
    [DataRow("csharp")]
    [DataRow("javascript")]
    [DataRow("typescript")]
    public void ConfigureFor_KnownLanguage_ComposesWithoutThrowing(string langValue)
    {
        Assert.IsTrue(SupportedLanguage.TryFromValue(langValue, out var lang));
        var pipeline = NewPipeline(lang);

        LanguageDefaults.ConfigureFor(pipeline, lang);
    }

    private static CompilationPipeline NewPipeline(SupportedLanguage lang)
    {
        var sp = CompileFixture.BuildServices();
        var registry = sp.GetRequiredService<ILanguageRegistry>();
        var irValidator = sp.GetRequiredService<IIrValidator>();
        return CompilationPipeline.For(registry, irValidator, lang, new Dictionary<string, string>());
    }
}
