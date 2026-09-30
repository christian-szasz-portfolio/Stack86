namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A scope-resolution expression (e.g. <c>Namespace::name</c> or <c>Class::member</c>).
/// </summary>
public sealed record CppScopeResolutionExpression : CppExpressionNode
{
    /// <summary>Left-hand scope name (namespace or class).</summary>
    public required string Scope { get; init; }

    /// <summary>Right-hand member name.</summary>
    public required string Member { get; init; }
}
