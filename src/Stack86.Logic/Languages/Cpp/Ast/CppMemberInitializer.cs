namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A member initializer in a constructor initializer list (e.g. <c>x(value)</c> or <c>Base(a, b)</c>).
/// </summary>
public sealed record CppMemberInitializer : CppAstNode
{
    /// <summary>The member or base class being initialised.</summary>
    public required string MemberName { get; init; }

    /// <summary>The initialiser arguments.</summary>
    public required IReadOnlyList<CppExpressionNode> Arguments { get; init; }
}
