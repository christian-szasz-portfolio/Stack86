namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A C++ constructor declaration.
/// </summary>
public sealed record CppConstructorDeclaration : CppDeclarationNode
{
    /// <summary>The class name this constructor belongs to.</summary>
    public required string ClassName { get; init; }

    /// <summary>Constructor parameters.</summary>
    public required IReadOnlyList<CppParameterDeclaration> Parameters { get; init; }

    /// <summary>Member initializer list entries (e.g. <c>x(val)</c>).</summary>
    public IReadOnlyList<CppMemberInitializer> Initializers { get; init; } = [];

    /// <summary>Constructor body.</summary>
    public CppBlockStatement? Body { get; init; }
}
