namespace Stack86.Logic.Languages.JavaScript.Ast;

/// <summary>
/// An update expression: <c>++x</c>, <c>--x</c>, <c>x++</c>, <c>x--</c>.
/// </summary>
public sealed record JsUpdateExpression : JsExpressionNode
{
    /// <summary>The operand being incremented or decremented.</summary>
    public required JsExpressionNode Operand { get; init; }

    /// <summary>Whether this is an increment (<c>true</c>) or decrement (<c>false</c>).</summary>
    public required bool IsIncrement { get; init; }

    /// <summary>Whether the operator is prefix (<c>true</c>) or postfix (<c>false</c>).</summary>
    public required bool IsPrefix { get; init; }
}
