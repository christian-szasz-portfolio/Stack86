namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A C++ class declaration with optional base class and access-sectioned members.
/// </summary>
public sealed record CppClassDeclaration : CppDeclarationNode
{
    /// <summary>Class name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional base class name for single inheritance.</summary>
    public string? BaseClassName { get; init; }

    /// <summary>Access modifier for base class inheritance (default: private).</summary>
    public CppAccessModifier BaseAccess { get; init; } = CppAccessModifier.Private;

    /// <summary>Access-sectioned member groups.</summary>
    public required IReadOnlyList<CppAccessSection> Sections { get; init; }
}
