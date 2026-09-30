namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A reference type (e.g. <c>int&amp;</c>). Transpiled to pointer in C output.
/// </summary>
public sealed record CppReferenceType : CppTypeNode
{
    /// <summary>The inner (referred-to) type.</summary>
    public required CppTypeNode Inner { get; init; }

    /// <summary>Whether the type is const.</summary>
    public bool IsConst { get; init; }
}
