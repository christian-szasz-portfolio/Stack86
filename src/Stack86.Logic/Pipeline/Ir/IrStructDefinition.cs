namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Describes a struct type definition to be emitted in the assembly output.
/// </summary>
/// <param name="Name">The struct type name.</param>
/// <param name="Fields">The fields of the struct, in declaration order.</param>
public sealed record IrStructDefinition(string Name, IReadOnlyList<IrStructField> Fields);
