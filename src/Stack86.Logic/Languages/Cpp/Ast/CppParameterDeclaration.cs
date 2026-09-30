namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A function or method parameter with optional default value.
/// </summary>
public sealed record CppParameterDeclaration : CppDeclarationNode
{
    /// <summary>Parameter type.</summary>
    public required CppTypeNode Type { get; init; }

    /// <summary>Parameter name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional default value expression.</summary>
    public CppExpressionNode? DefaultValue { get; init; }
}
