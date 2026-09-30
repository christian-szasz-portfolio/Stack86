namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A C++ template declaration wrapping an inner declaration (function or class).
/// Only single type parameter templates are supported.
/// </summary>
public sealed record CppTemplateDeclaration : CppDeclarationNode
{
    /// <summary>Type parameter names (e.g. <c>["T"]</c>).</summary>
    public required IReadOnlyList<string> TypeParameters { get; init; }

    /// <summary>The templated declaration (function or class).</summary>
    public required CppDeclarationNode Inner { get; init; }
}
