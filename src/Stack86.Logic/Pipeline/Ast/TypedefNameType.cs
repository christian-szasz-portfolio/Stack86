namespace Stack86.Logic.Pipeline.Ast;

/// <summary>
/// A reference to a typedef alias name (e.g. <c>i16</c> after <c>typedef int i16;</c>).
/// </summary>
public sealed record TypedefNameType : TypeNode
{
    /// <summary>Gets the typedef alias name.</summary>
    public required string Name { get; init; }
}
