namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// An array type (e.g. <c>int[10]</c>).
/// </summary>
public sealed record CppArrayType : CppTypeNode
{
    /// <summary>Element type.</summary>
    public required CppTypeNode ElementType { get; init; }

    /// <summary>Array size expression, or <c>null</c> for unsized arrays.</summary>
    public CppExpressionNode? Size { get; init; }
}
