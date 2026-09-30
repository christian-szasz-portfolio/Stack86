namespace Stack86.Logic.Test.Languages.C;

using Stack86.Logic.Languages.C;

/// <summary>
/// Unit tests for the C type-info POCO hierarchy: <see cref="CPrimitiveTypeInfo"/>,
/// <see cref="CPointerTypeInfo"/>, <see cref="CArrayTypeInfo"/>, <see cref="CStructTypeInfo"/>,
/// <see cref="CUnionTypeInfo"/>, <see cref="CEnumTypeInfo"/>, <see cref="CFunctionPointerTypeInfo"/>,
/// <see cref="CStructField"/>, and <see cref="CEnumMember"/>.
/// </summary>
[TestClass]
public sealed class CTypeInfoTests
{
    [TestMethod]
    public void CPrimitive_IntSingleton_HasExpectedKindAndSize()
    {
        Assert.AreEqual(CPrimitiveKind.Int, CPrimitiveTypeInfo.Int.Kind);
        Assert.AreEqual(2, CPrimitiveTypeInfo.Int.SizeInBytes);
        Assert.IsTrue(CPrimitiveTypeInfo.Int.IsScalar);
    }

    [TestMethod]
    public void CPrimitive_CharSingleton_HasExpectedKindAndSize()
    {
        Assert.AreEqual(CPrimitiveKind.Char, CPrimitiveTypeInfo.Char.Kind);
        Assert.AreEqual(1, CPrimitiveTypeInfo.Char.SizeInBytes);
    }

    [TestMethod]
    public void CPrimitive_VoidSingleton_HasZeroSize()
    {
        Assert.AreEqual(CPrimitiveKind.Void, CPrimitiveTypeInfo.Void.Kind);
        Assert.AreEqual(0, CPrimitiveTypeInfo.Void.SizeInBytes);
    }

    [TestMethod]
    public void CPointer_IsTwoBytesAndScalar()
    {
        var pointer = new CPointerTypeInfo(CPrimitiveTypeInfo.Char);

        Assert.AreSame(CPrimitiveTypeInfo.Char, pointer.Inner);
        Assert.AreEqual(2, pointer.SizeInBytes);
        Assert.IsTrue(pointer.IsScalar);
    }

    [TestMethod]
    public void CArray_SizeIsElementTimesLength_NonScalar()
    {
        var array = new CArrayTypeInfo(CPrimitiveTypeInfo.Int, 5);

        Assert.AreSame(CPrimitiveTypeInfo.Int, array.Element);
        Assert.AreEqual(5, array.Length);
        Assert.AreEqual(10, array.SizeInBytes);
        Assert.IsFalse(array.IsScalar);
    }

    [TestMethod]
    public void CArray_OfChars_SizeMatchesLength()
    {
        var array = new CArrayTypeInfo(CPrimitiveTypeInfo.Char, 7);

        Assert.AreEqual(7, array.SizeInBytes);
    }

    [TestMethod]
    public void CArray_ZeroLength_HasZeroSize()
    {
        var array = new CArrayTypeInfo(CPrimitiveTypeInfo.Int, 0);

        Assert.AreEqual(0, array.SizeInBytes);
    }

    [TestMethod]
    public void CStructField_StoresNameAndType()
    {
        var field = new CStructField("x", CPrimitiveTypeInfo.Int);

        Assert.AreEqual("x", field.Name);
        Assert.AreSame(CPrimitiveTypeInfo.Int, field.Type);
    }

    [TestMethod]
    public void CStruct_SizeSumsFieldSizes_NonScalar()
    {
        var fields = new List<CStructField>
        {
            new("a", CPrimitiveTypeInfo.Int),
            new("b", CPrimitiveTypeInfo.Char),
            new("c", CPrimitiveTypeInfo.Int),
        };
        var s = new CStructTypeInfo("Point", fields);

        Assert.AreEqual("Point", s.Name);
        Assert.HasCount(3, s.Fields);
        Assert.AreEqual(5, s.SizeInBytes);
        Assert.IsFalse(s.IsScalar);
    }

    [TestMethod]
    public void CStruct_TryGetField_FoundReturnsOffsetAndType()
    {
        var fields = new List<CStructField>
        {
            new("a", CPrimitiveTypeInfo.Int),  // offset 0
            new("b", CPrimitiveTypeInfo.Char), // offset 2
            new("c", CPrimitiveTypeInfo.Int),  // offset 3
        };
        var s = new CStructTypeInfo("S", fields);

        Assert.IsTrue(s.TryGetField("a", out var aOff, out var aType));
        Assert.AreEqual(0, aOff);
        Assert.AreSame(CPrimitiveTypeInfo.Int, aType);

        Assert.IsTrue(s.TryGetField("b", out var bOff, out var bType));
        Assert.AreEqual(2, bOff);
        Assert.AreSame(CPrimitiveTypeInfo.Char, bType);

        Assert.IsTrue(s.TryGetField("c", out var cOff, out _));
        Assert.AreEqual(3, cOff);
    }

    [TestMethod]
    public void CStruct_TryGetField_MissingReturnsFalse()
    {
        var fields = new List<CStructField> { new("only", CPrimitiveTypeInfo.Int) };
        var s = new CStructTypeInfo("S", fields);

        Assert.IsFalse(s.TryGetField("missing", out var off, out var type));
        Assert.AreEqual(0, off);
        Assert.AreSame(CPrimitiveTypeInfo.Void, type);
    }

    [TestMethod]
    public void CUnion_SizeIsMaxField_NonScalar()
    {
        var fields = new List<CStructField>
        {
            new("c", CPrimitiveTypeInfo.Char),
            new("i", CPrimitiveTypeInfo.Int),
            new("a", new CArrayTypeInfo(CPrimitiveTypeInfo.Char, 4)),
        };
        var u = new CUnionTypeInfo("U", fields);

        Assert.AreEqual("U", u.Name);
        Assert.HasCount(3, u.Fields);
        Assert.AreEqual(4, u.SizeInBytes);
        Assert.IsFalse(u.IsScalar);
    }

    [TestMethod]
    public void CUnion_EmptyFields_SizeIsZero()
    {
        var u = new CUnionTypeInfo("Empty", []);

        Assert.AreEqual(0, u.SizeInBytes);
    }

    [TestMethod]
    public void CUnion_TryGetField_FoundAndMissing()
    {
        var u = new CUnionTypeInfo(
            "U",
            [new CStructField("a", CPrimitiveTypeInfo.Int)]);

        Assert.IsTrue(u.TryGetField("a", out var foundType));
        Assert.AreSame(CPrimitiveTypeInfo.Int, foundType);

        Assert.IsFalse(u.TryGetField("b", out var missingType));
        Assert.AreSame(CPrimitiveTypeInfo.Void, missingType);
    }

    [TestMethod]
    public void CEnumMember_StoresNameAndValue()
    {
        var m = new CEnumMember("Red", 7);

        Assert.AreEqual("Red", m.Name);
        Assert.AreEqual(7, m.Value);
    }

    [TestMethod]
    public void CEnumType_SizeIsTwoBytes_AndExposesMembers()
    {
        var members = new List<CEnumMember>
        {
            new("A", 0),
            new("B", 1),
            new("C", 42),
        };
        var e = new CEnumTypeInfo("Color", members);

        Assert.AreEqual("Color", e.Name);
        Assert.HasCount(3, e.Members);
        Assert.AreEqual(2, e.SizeInBytes);
        Assert.IsTrue(e.IsScalar);
    }

    [TestMethod]
    public void CEnumType_TryGetValue_FoundReturnsMemberValue()
    {
        var e = new CEnumTypeInfo(
            "E",
            [new CEnumMember("FIRST", 1), new CEnumMember("SECOND", 2)]);

        Assert.IsTrue(e.TryGetValue("SECOND", out var v));
        Assert.AreEqual(2, v);
    }

    [TestMethod]
    public void CEnumType_TryGetValue_MissingReturnsFalseAndZero()
    {
        var e = new CEnumTypeInfo("E", [new CEnumMember("A", 1)]);

        Assert.IsFalse(e.TryGetValue("MISSING", out var v));
        Assert.AreEqual(0, v);
    }

    [TestMethod]
    public void CEnumType_TryGetValue_EmptyMembers_ReturnsFalse()
    {
        var e = new CEnumTypeInfo("E", []);

        Assert.IsFalse(e.TryGetValue("anything", out var v));
        Assert.AreEqual(0, v);
    }

    [TestMethod]
    public void CFunctionPointer_IsTwoBytesAndStoresSignature()
    {
        var fp = new CFunctionPointerTypeInfo(
            CPrimitiveTypeInfo.Int,
            [CPrimitiveTypeInfo.Int, CPrimitiveTypeInfo.Char]);

        Assert.AreSame(CPrimitiveTypeInfo.Int, fp.ReturnType);
        Assert.HasCount(2, fp.ParameterTypes);
        Assert.AreEqual(2, fp.SizeInBytes);
        Assert.IsTrue(fp.IsScalar);
    }
}
