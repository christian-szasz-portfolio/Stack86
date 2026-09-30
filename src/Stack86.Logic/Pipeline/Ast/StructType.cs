namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A struct type reference: <c>struct Name</c> used in variable or parameter declarations.
/// </summary>
public sealed record StructType : TypeNode
{
    /// <summary>Gets the name of the struct being referenced.</summary>
    public required string Name { get; init; }
}
