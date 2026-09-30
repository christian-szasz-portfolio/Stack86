namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A named type reference (class name, struct name, typedef name, or template type parameter).
/// </summary>
public sealed record CppNamedType : CppTypeNode
{
    /// <summary>The type name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional template type arguments for template instantiation.</summary>
    public IReadOnlyList<CppTypeNode> TypeArguments { get; init; } = [];

    /// <summary>Whether the type is const.</summary>
    public bool IsConst { get; init; }
}
