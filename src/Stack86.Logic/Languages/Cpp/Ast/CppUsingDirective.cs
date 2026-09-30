namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A <c>using namespace</c> directive (e.g. <c>using namespace std;</c>).
/// </summary>
public sealed record CppUsingDirective : CppDeclarationNode
{
    /// <summary>The namespace being brought into scope.</summary>
    public required string NamespaceName { get; init; }
}
