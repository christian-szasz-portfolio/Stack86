namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A C++ method (member function) declaration.
/// </summary>
public sealed record CppMethodDeclaration : CppDeclarationNode
{
    /// <summary>Return type of the method.</summary>
    public required CppTypeNode ReturnType { get; init; }

    /// <summary>Method name.</summary>
    public required string Name { get; init; }

    /// <summary>Method parameters.</summary>
    public required IReadOnlyList<CppParameterDeclaration> Parameters { get; init; }

    /// <summary>Method body, or <c>null</c> for a declaration without body.</summary>
    public CppBlockStatement? Body { get; init; }

    /// <summary>Whether this method is marked <c>virtual</c>.</summary>
    public bool IsVirtual { get; init; }

    /// <summary>Whether this method is marked <c>override</c>.</summary>
    public bool IsOverride { get; init; }

    /// <summary>Whether this method is marked <c>const</c>.</summary>
    public bool IsConst { get; init; }
}
