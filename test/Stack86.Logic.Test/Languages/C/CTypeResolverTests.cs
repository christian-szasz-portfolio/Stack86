namespace Stack86.Logic.Test.Languages.C;

using Stack86.Common.Exceptions;
using Stack86.Logic.Languages.C;
using Stack86.Logic.Pipeline.Ast;

/// <summary>
/// Unit tests for <see cref="CTypeResolver"/> covering primitive resolution, pointer/array
/// composition, struct/union/enum registration, typedef lookup, error paths and
/// <see cref="CTypeResolver.SizeOf"/>.
/// </summary>
[TestClass]
public sealed class CTypeResolverTests
{
    [TestMethod]
    public void Resolve_PrimitiveInt_ReturnsIntSingleton()
    {
        var resolver = new CTypeResolver();
        var node = new PrimitiveType { Kind = PrimitiveKind.Int };

        Assert.AreSame(CPrimitiveTypeInfo.Int, resolver.Resolve(node));
    }

    [TestMethod]
    public void Resolve_PrimitiveChar_ReturnsCharSingleton()
    {
        var resolver = new CTypeResolver();
        var node = new PrimitiveType { Kind = PrimitiveKind.Char };

        Assert.AreSame(CPrimitiveTypeInfo.Char, resolver.Resolve(node));
    }

    [TestMethod]
    public void Resolve_PrimitiveVoid_ReturnsVoidSingleton()
    {
        var resolver = new CTypeResolver();
        var node = new PrimitiveType { Kind = PrimitiveKind.Void };

        Assert.AreSame(CPrimitiveTypeInfo.Void, resolver.Resolve(node));
    }

    [TestMethod]
    public void Resolve_Pointer_WrapsInner()
    {
        var resolver = new CTypeResolver();
        var node = new PointerType { Inner = new PrimitiveType { Kind = PrimitiveKind.Char } };

        var resolved = resolver.Resolve(node);

        var ptr = (CPointerTypeInfo)resolved;
        Assert.AreSame(CPrimitiveTypeInfo.Char, ptr.Inner);
    }

    [TestMethod]
    public void Resolve_Array_BuildsCArrayTypeInfo()
    {
        var resolver = new CTypeResolver();
        var node = new ArrayType
        {
            ElementType = new PrimitiveType { Kind = PrimitiveKind.Int },
            Size = 8,
        };

        var arr = (CArrayTypeInfo)resolver.Resolve(node);

        Assert.AreEqual(8, arr.Length);
        Assert.AreSame(CPrimitiveTypeInfo.Int, arr.Element);
    }

    [TestMethod]
    public void Resolve_Struct_AfterRegistration_ReturnsRegisteredInstance()
    {
        var resolver = new CTypeResolver();
        var fields = new List<StructFieldDeclaration>
        {
            new() { Name = "x", Type = new PrimitiveType { Kind = PrimitiveKind.Int } },
            new() { Name = "y", Type = new PrimitiveType { Kind = PrimitiveKind.Int } },
        };
        var registered = resolver.RegisterStruct("Point", fields);

        var resolved = (CStructTypeInfo)resolver.Resolve(new StructType { Name = "Point" });

        Assert.AreSame(registered, resolved);
        Assert.HasCount(2, resolved.Fields);
        Assert.AreEqual(4, resolved.SizeInBytes);
        CollectionAssert.Contains(resolver.Structs.Keys.ToList(), "Point");
    }

    [TestMethod]
    public void RegisterStruct_TwiceWithSameName_RetainsSameInstanceForSelfReferentialTypes()
    {
        var resolver = new CTypeResolver();

        // First registration: forward declaration for self-referential pointer.
        var first = resolver.RegisterStruct("Node", []);

        // Pointer-to-Node resolves now (uses forward-declared instance).
        var ptr = (CPointerTypeInfo)resolver.Resolve(new PointerType { Inner = new StructType { Name = "Node" } });
        Assert.AreSame(first, ptr.Inner);

        // Second registration with real fields.
        var second = resolver.RegisterStruct(
            "Node",
            [
                new StructFieldDeclaration { Name = "value", Type = new PrimitiveType { Kind = PrimitiveKind.Int } },
                new StructFieldDeclaration { Name = "next", Type = new PointerType { Inner = new StructType { Name = "Node" } } },
            ]);

        Assert.AreSame(first, second);
        Assert.HasCount(2, second.Fields);
    }

    [TestMethod]
    public void Resolve_StructUnknown_Throws()
    {
        var resolver = new CTypeResolver();

        Assert.ThrowsExactly<UnknownTypeException>(
            () => resolver.Resolve(new StructType { Name = "Missing" }));
    }

    [TestMethod]
    public void RegisterUnion_AndResolve_ProducesCUnionTypeInfo()
    {
        var resolver = new CTypeResolver();
        resolver.RegisterUnion(
            "U",
            [
                new StructFieldDeclaration { Name = "i", Type = new PrimitiveType { Kind = PrimitiveKind.Int } },
                new StructFieldDeclaration { Name = "c", Type = new PrimitiveType { Kind = PrimitiveKind.Char } },
            ]);

        var u = (CUnionTypeInfo)resolver.Resolve(new UnionType { Name = "U" });

        Assert.HasCount(2, u.Fields);
        Assert.AreEqual(2, u.SizeInBytes);
    }

    [TestMethod]
    public void Resolve_UnionUnknown_Throws()
    {
        var resolver = new CTypeResolver();

        Assert.ThrowsExactly<UnknownTypeException>(
            () => resolver.Resolve(new UnionType { Name = "Missing" }));
    }

    [TestMethod]
    public void RegisterEnum_AndResolve_ProducesCEnumTypeInfo()
    {
        var resolver = new CTypeResolver();
        resolver.RegisterEnum(
            "Color",
            [new CEnumMember("Red", 0), new CEnumMember("Green", 1)]);

        var e = (CEnumTypeInfo)resolver.Resolve(new EnumType { Name = "Color" });
        Assert.HasCount(2, e.Members);
    }

    [TestMethod]
    public void Resolve_EnumUnknown_Throws()
    {
        var resolver = new CTypeResolver();

        Assert.ThrowsExactly<UnknownTypeException>(
            () => resolver.Resolve(new EnumType { Name = "Missing" }));
    }

    [TestMethod]
    public void TryResolveEnumConstant_FoundAcrossEnums()
    {
        var resolver = new CTypeResolver();
        resolver.RegisterEnum("A", [new CEnumMember("X", 1)]);
        resolver.RegisterEnum("B", [new CEnumMember("Y", 42)]);

        Assert.IsTrue(resolver.TryResolveEnumConstant("Y", out var v));
        Assert.AreEqual(42, v);
    }

    [TestMethod]
    public void TryResolveEnumConstant_NotFound_ReturnsFalseAndZero()
    {
        var resolver = new CTypeResolver();
        resolver.RegisterEnum("A", [new CEnumMember("X", 1)]);

        Assert.IsFalse(resolver.TryResolveEnumConstant("Z", out var v));
        Assert.AreEqual(0, v);
    }

    [TestMethod]
    public void Typedef_RegisterAndResolve_ReturnsTarget()
    {
        var resolver = new CTypeResolver();
        resolver.RegisterTypedef("i16", CPrimitiveTypeInfo.Int);

        Assert.IsTrue(resolver.IsTypedefName("i16"));
        Assert.IsFalse(resolver.IsTypedefName("nope"));

        Assert.IsTrue(resolver.TryResolveTypedef("i16", out var t));
        Assert.AreSame(CPrimitiveTypeInfo.Int, t);

        var resolved = resolver.Resolve(new TypedefNameType { Name = "i16" });
        Assert.AreSame(CPrimitiveTypeInfo.Int, resolved);
    }

    [TestMethod]
    public void TryResolveTypedef_Missing_ReturnsFalse()
    {
        var resolver = new CTypeResolver();

        Assert.IsFalse(resolver.TryResolveTypedef("missing", out _));
    }

    [TestMethod]
    public void Resolve_TypedefUnknown_Throws()
    {
        var resolver = new CTypeResolver();

        Assert.ThrowsExactly<UnknownTypeException>(
            () => resolver.Resolve(new TypedefNameType { Name = "nope" }));
    }

    [TestMethod]
    public void SizeOf_DelegatesToResolvedType()
    {
        var resolver = new CTypeResolver();

        Assert.AreEqual(2, resolver.SizeOf(new PrimitiveType { Kind = PrimitiveKind.Int }));
        Assert.AreEqual(1, resolver.SizeOf(new PrimitiveType { Kind = PrimitiveKind.Char }));
        Assert.AreEqual(
            10,
            resolver.SizeOf(new ArrayType
            {
                ElementType = new PrimitiveType { Kind = PrimitiveKind.Int },
                Size = 5,
            }));
    }

    [TestMethod]
    public void Resolve_UnknownNodeKind_Throws()
    {
        var resolver = new CTypeResolver();

        Assert.ThrowsExactly<CompilerInvariantException>(() => resolver.Resolve(new UnknownTypeNode()));
    }

    private sealed record UnknownTypeNode : TypeNode;
}
