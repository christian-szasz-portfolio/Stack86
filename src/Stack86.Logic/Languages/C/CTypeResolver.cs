namespace Stack86.Logic.Languages.C;

using Stack86.Common.Exceptions;
using Stack86.Logic.Pipeline.Ast;

/// <summary>
/// Resolves AST <see cref="TypeNode"/> instances to semantic <see cref="CTypeInfo"/> objects.
/// Maintains registries for structs, unions, enums, and typedefs.
/// </summary>
public sealed class CTypeResolver
{
    private readonly Dictionary<string, CStructTypeInfo> structs = [];
    private readonly Dictionary<string, CUnionTypeInfo> unions = [];
    private readonly Dictionary<string, CEnumTypeInfo> enums = [];
    private readonly Dictionary<string, CTypeInfo> typedefs = [];

    /// <summary>
    /// Gets all registered struct type definitions.
    /// </summary>
    public IReadOnlyDictionary<string, CStructTypeInfo> Structs => this.structs;

    /// <summary>
    /// Resolves an AST <see cref="TypeNode"/> to the corresponding <see cref="CTypeInfo"/>.
    /// </summary>
    /// <param name="node">The AST type node to resolve.</param>
    /// <returns>The resolved type information.</returns>
    public CTypeInfo Resolve(TypeNode node)
    {
        return node switch
        {
            PrimitiveType p => ResolvePrimitive(p),
            PointerType ptr => new CPointerTypeInfo(this.Resolve(ptr.Inner)),
            StructType st => this.ResolveStructRef(st.Name),
            EnumType et => this.ResolveEnumRef(et.Name),
            UnionType ut => this.ResolveUnionRef(ut.Name),
            ArrayType arr => this.ResolveArray(arr),
            TypedefNameType td => this.ResolveTypedefName(td.Name),
            FunctionPointerType fp => new CFunctionPointerTypeInfo(
                this.Resolve(fp.ReturnType),
                [.. fp.Parameters.Select(this.Resolve)]),
            _ => throw new CompilerInvariantException($"Cannot resolve type node: {node.GetType().Name}"),
        };
    }

    /// <summary>
    /// Registers a struct declaration so it can be referenced later.
    /// </summary>
    /// <param name="name">The struct tag name.</param>
    /// <param name="fieldDecls">The AST field declarations.</param>
    /// <returns>The registered struct type info.</returns>
    public CStructTypeInfo RegisterStruct(string name, IReadOnlyList<StructFieldDeclaration> fieldDecls)
    {
        // Pre-register with empty fields so self-referential pointer types
        // (e.g. struct Node* inside struct Node) resolve to the same object.
        if (!this.structs.TryGetValue(name, out var info))
        {
            info = new CStructTypeInfo(name, []);
            this.structs[name] = info;
        }

        if (fieldDecls.Count > 0)
        {
            var fields = new List<CStructField>(fieldDecls.Count);
            foreach (var f in fieldDecls)
            {
                fields.Add(new CStructField(f.Name, this.Resolve(f.Type)));
            }

            info.SetFields(fields);
        }

        return info;
    }

    /// <summary>
    /// Registers a union declaration.
    /// </summary>
    /// <param name="name">The union tag name.</param>
    /// <param name="fieldDecls">The AST field declarations.</param>
    /// <returns>The registered union type info.</returns>
    public CUnionTypeInfo RegisterUnion(string name, IReadOnlyList<StructFieldDeclaration> fieldDecls)
    {
        var fields = new List<CStructField>(fieldDecls.Count);
        foreach (var f in fieldDecls)
        {
            fields.Add(new CStructField(f.Name, this.Resolve(f.Type)));
        }

        var info = new CUnionTypeInfo(name, fields);
        this.unions[name] = info;
        return info;
    }

    /// <summary>
    /// Registers an enum declaration.
    /// </summary>
    /// <param name="name">The enum tag name.</param>
    /// <param name="members">The list of enum members with their values.</param>
    /// <returns>The registered enum type info.</returns>
    public CEnumTypeInfo RegisterEnum(string name, IReadOnlyList<CEnumMember> members)
    {
        var info = new CEnumTypeInfo(name, members);
        this.enums[name] = info;
        return info;
    }

    /// <summary>
    /// Registers a typedef alias.
    /// </summary>
    /// <param name="alias">The alias name.</param>
    /// <param name="target">The underlying type.</param>
    public void RegisterTypedef(string alias, CTypeInfo target)
    {
        this.typedefs[alias] = target;
    }

    /// <summary>
    /// Tries to resolve a name as a typedef alias.
    /// </summary>
    /// <param name="name">The identifier to look up.</param>
    /// <param name="type">The resolved type if found.</param>
    /// <returns><see langword="true"/> if the name is a registered typedef.</returns>
    public bool TryResolveTypedef(string name, out CTypeInfo type)
    {
        return this.typedefs.TryGetValue(name, out type!);
    }

    /// <summary>
    /// Tries to look up an enum constant across all registered enums.
    /// </summary>
    /// <param name="name">The constant name.</param>
    /// <param name="value">The integer value if found.</param>
    /// <returns><see langword="true"/> if the name is a known enum constant.</returns>
    public bool TryResolveEnumConstant(string name, out int value)
    {
        foreach (var enumInfo in this.enums.Values)
        {
            if (enumInfo.TryGetValue(name, out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="name"/> is a registered typedef.
    /// </summary>
    /// <param name="name">The identifier to check.</param>
    /// <returns><see langword="true"/> when the name is a known typedef.</returns>
    public bool IsTypedefName(string name) => this.typedefs.ContainsKey(name);

    /// <summary>
    /// Returns the size in bytes of the type that a given <see cref="TypeNode"/> resolves to.
    /// </summary>
    /// <param name="node">The AST type node.</param>
    /// <returns>Size in bytes.</returns>
    public int SizeOf(TypeNode node) => this.Resolve(node).SizeInBytes;

    private static CPrimitiveTypeInfo ResolvePrimitive(PrimitiveType p)
    {
        return p.Kind switch
        {
            PrimitiveKind.Int => CPrimitiveTypeInfo.Int,
            PrimitiveKind.Char => CPrimitiveTypeInfo.Char,
            PrimitiveKind.Void => CPrimitiveTypeInfo.Void,
            _ => throw new CompilerInvariantException($"Unknown primitive kind: {p.Kind}"),
        };
    }

    private CStructTypeInfo ResolveStructRef(string name)
    {
        if (this.structs.TryGetValue(name, out var info))
        {
            return info;
        }

        throw new UnknownTypeException($"Unknown struct type: '{name}'.");
    }

    private CEnumTypeInfo ResolveEnumRef(string name)
    {
        if (this.enums.TryGetValue(name, out var info))
        {
            return info;
        }

        throw new UnknownTypeException($"Unknown enum type: '{name}'.");
    }

    private CUnionTypeInfo ResolveUnionRef(string name)
    {
        if (this.unions.TryGetValue(name, out var info))
        {
            return info;
        }

        throw new UnknownTypeException($"Unknown union type: '{name}'.");
    }

    private CArrayTypeInfo ResolveArray(ArrayType arr)
    {
        var element = this.Resolve(arr.ElementType);
        return new CArrayTypeInfo(element, arr.Size);
    }

    private CTypeInfo ResolveTypedefName(string name)
    {
        if (this.typedefs.TryGetValue(name, out var info))
        {
            return info;
        }

        throw new UnknownTypeException($"Unknown typedef name: '{name}'.");
    }
}
