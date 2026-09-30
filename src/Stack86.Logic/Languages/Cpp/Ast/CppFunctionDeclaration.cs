namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A function declaration (free function, not a class method).
/// </summary>
public sealed record CppFunctionDeclaration : CppDeclarationNode
{
    /// <summary>Return type.</summary>
    public required CppTypeNode ReturnType { get; init; }

    /// <summary>Function name (may be scope-qualified via <c>::</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Optional class scope for out-of-class definitions (e.g. <c>Point::move</c>).</summary>
    public string? ClassScope { get; init; }

    /// <summary>Function parameters.</summary>
    public required IReadOnlyList<CppParameterDeclaration> Parameters { get; init; }

    /// <summary>Function body, or <c>null</c> for a prototype.</summary>
    public CppBlockStatement? Body { get; init; }
}
