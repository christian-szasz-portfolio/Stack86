namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A member access expression: <c>expr.field</c> (dot) or <c>expr-&gt;field</c> (arrow).
/// </summary>
public sealed record MemberAccessExpression : ExpressionNode
{
    /// <summary>Gets the object expression being accessed.</summary>
    public required ExpressionNode Object { get; init; }

    /// <summary>Gets the name of the member being accessed.</summary>
    public required string MemberName { get; init; }

    /// <summary>Gets a value indicating whether this is an arrow (<c>-&gt;</c>) access rather than dot (<c>.</c>).</summary>
    public required bool IsArrow { get; init; }
}
