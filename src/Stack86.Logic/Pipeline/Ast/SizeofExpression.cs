namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A sizeof expression: <c>sizeof(type)</c> or <c>sizeof(expr)</c>.
/// </summary>
public sealed record SizeofExpression : ExpressionNode
{
    /// <summary>
    /// Gets the target type when sizeof is applied to a type.
    /// Mutually exclusive with <see cref="Operand"/>.
    /// </summary>
    public TypeNode? TargetType { get; init; }

    /// <summary>
    /// Gets the operand when sizeof is applied to an expression.
    /// Mutually exclusive with <see cref="TargetType"/>.
    /// </summary>
    public ExpressionNode? Operand { get; init; }
}
