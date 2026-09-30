namespace Stack86.Logic.Test.Languages;

using Stack86.Logic.Pipeline.X86Conversion;

[TestClass]
public sealed class X8086CapabilityProfileTests
{
    [TestMethod]
    public void Properties_HaveExpectedValues()
    {
        var p = new X8086CapabilityProfile();
        Assert.AreEqual("8086", p.TargetName);
        Assert.AreEqual(32767, p.MaxIntegerValue);
        Assert.AreEqual(-32768, p.MinIntegerValue);
        Assert.AreEqual(2, p.IntegerSizeBytes);
        Assert.IsFalse(p.SupportsFloatingPoint);
        Assert.IsFalse(p.SupportsWideIntegers);
        Assert.IsFalse(p.SupportsFileIo);
        Assert.Contains("float", p.UnsupportedCTypeKeywords);
        Assert.Contains("const", p.UnsupportedCQualifierKeywords);
    }
}
