namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A C++ destructor declaration.
/// </summary>
public sealed record CppDestructorDeclaration : CppDeclarationNode
{
    /// <summary>The class name this destructor belongs to.</summary>
    public required string ClassName { get; init; }

    /// <summary>Destructor body.</summary>
    public CppBlockStatement? Body { get; init; }
}
