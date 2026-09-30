namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Describes a field within an IR struct definition.
/// </summary>
/// <param name="Name">The field name.</param>
/// <param name="SizeInBytes">The size of the field in bytes.</param>
public sealed record IrStructField(string Name, int SizeInBytes);
