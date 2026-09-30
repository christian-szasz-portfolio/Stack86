namespace Stack86.Logic.Test.Languages;

using Stack86.Logic.Languages;

/// <summary>
/// Unit tests for the <see cref="SupportedLanguage"/> registry sealed-string enum.
/// </summary>
[TestClass]
public sealed class SupportedLanguageTests
{
    [TestMethod]
    public void Singletons_ExposeExpectedValues()
    {
        Assert.AreEqual("c", SupportedLanguage.C.Value);
        Assert.AreEqual("cpp", SupportedLanguage.Cpp.Value);
        Assert.AreEqual("csharp", SupportedLanguage.CSharp.Value);
        Assert.AreEqual("javascript", SupportedLanguage.JavaScript.Value);
        Assert.AreEqual("typescript", SupportedLanguage.TypeScript.Value);
    }

    [TestMethod]
    public void ToString_ReturnsValue()
    {
        Assert.AreEqual("c", SupportedLanguage.C.ToString());
        Assert.AreEqual("csharp", SupportedLanguage.CSharp.ToString());
    }

    [TestMethod]
    public void GetAll_ContainsAllSupportedLanguages()
    {
        var all = SupportedLanguage.GetAll();

        Assert.HasCount(5, all);
        CollectionAssert.Contains(all.ToList(), SupportedLanguage.C);
        CollectionAssert.Contains(all.ToList(), SupportedLanguage.CSharp);
        CollectionAssert.Contains(all.ToList(), SupportedLanguage.TypeScript);
    }

    [TestMethod]
    [DataRow("c")]
    [DataRow("cpp")]
    [DataRow("csharp")]
    [DataRow("javascript")]
    [DataRow("typescript")]
    public void TryFromValue_KnownIdentifier_ReturnsTrueAndMatchingSingleton(string value)
    {
        Assert.IsTrue(SupportedLanguage.TryFromValue(value, out var language));
        Assert.IsNotNull(language);
        Assert.AreEqual(value, language.Value);
    }

    [TestMethod]
    [DataRow("C")]
    [DataRow("CSharp")]
    [DataRow("CSHARP")]
    public void TryFromValue_IsCaseInsensitive(string value)
    {
        Assert.IsTrue(SupportedLanguage.TryFromValue(value, out var language));
        Assert.IsNotNull(language);
    }

    [TestMethod]
    public void TryFromValue_UnknownIdentifier_ReturnsFalse()
    {
        Assert.IsFalse(SupportedLanguage.TryFromValue("klingon", out var language));
        Assert.IsNull(language);
    }
}
