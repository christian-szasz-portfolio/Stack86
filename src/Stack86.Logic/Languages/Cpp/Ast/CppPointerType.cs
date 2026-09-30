namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A pointer type (e.g. <c>int*</c>).
/// </summary>
public sealed record CppPointerType : CppTypeNode
{
    /// <summary>The inner (pointed-to) type.</summary>
    public required CppTypeNode Inner { get; init; }

    /// <summary>Whether the type is const.</summary>
    public bool IsConst { get; init; }
}
