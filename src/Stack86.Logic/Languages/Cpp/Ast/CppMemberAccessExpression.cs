namespace Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// A member access expression via <c>.</c> or <c>-&gt;</c>.
/// </summary>
public sealed record CppMemberAccessExpression : CppExpressionNode
{
    /// <summary>The object expression.</summary>
    public required CppExpressionNode Object { get; init; }

    /// <summary>The member name.</summary>
    public required string Member { get; init; }

    /// <summary>Whether access is via <c>-&gt;</c> (true) or <c>.</c> (false).</summary>
    public bool IsArrow { get; init; }
}
