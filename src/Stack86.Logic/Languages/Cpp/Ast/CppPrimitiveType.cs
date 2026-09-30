namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A primitive type (int, char, void, bool, short, long).
/// </summary>
public sealed record CppPrimitiveType : CppTypeNode
{
    /// <summary>The primitive kind.</summary>
    public required CppPrimitiveKind Kind { get; init; }

    /// <summary>Whether the type is unsigned.</summary>
    public bool IsUnsigned { get; init; }

    /// <summary>Whether the type is const.</summary>
    public bool IsConst { get; init; }
}
