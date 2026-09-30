namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Languages.C;

/// <summary>
/// Unit tests for <see cref="SymbolInfo"/>.
/// </summary>
[TestClass]
public sealed class SymbolInfoTests
{
    [TestMethod]
    public void Constructor_StoresAllValues()
    {
        var sym = new SymbolInfo(3, CPrimitiveTypeInfo.Int, IsGlobal: true, IsParameter: false);

        Assert.AreEqual(3, sym.Register);
        Assert.AreSame(CPrimitiveTypeInfo.Int, sym.Type);
        Assert.IsTrue(sym.IsGlobal);
        Assert.IsFalse(sym.IsParameter);
    }

    [TestMethod]
    public void RecordEquality_HoldsByValue()
    {
        var a = new SymbolInfo(0, CPrimitiveTypeInfo.Char, false, true);
        var b = new SymbolInfo(0, CPrimitiveTypeInfo.Char, false, true);
        var c = a with { Register = 1 };

        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
    }
}
