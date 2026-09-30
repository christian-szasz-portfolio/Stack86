namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A function pointer type, e.g. <c>int (*)(int, int)</c>.
/// </summary>
public sealed record FunctionPointerType : TypeNode
{
    /// <summary>Gets the return type of the pointed-to function.</summary>
    public required TypeNode ReturnType { get; init; }

    /// <summary>Gets the parameter types of the pointed-to function.</summary>
    public required IReadOnlyList<TypeNode> Parameters { get; init; }
}
