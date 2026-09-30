namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// An enum type reference: <c>enum Name</c> used in variable declarations.
/// </summary>
public sealed record EnumType : TypeNode
{
    /// <summary>Gets the name of the enum being referenced.</summary>
    public required string Name { get; init; }
}
