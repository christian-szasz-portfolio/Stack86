namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Describes a virtual method table to be emitted in the assembly data segment.
/// Each entry contains <c>DW</c> values: the addresses of the virtual method labels.
/// </summary>
/// <param name="Label">The assembly label for this vtable (e.g. <c>_vtbl_Dog</c>).</param>
/// <param name="MethodLabels">Ordered list of mangled function names constituting the vtable slots.</param>
public sealed record IrVtableEntry(string Label, IReadOnlyList<string> MethodLabels);
