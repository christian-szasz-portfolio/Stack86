namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// A labelled block of initialised data to place in the data segment.
/// </summary>
public sealed record IrGlobalData
{
    /// <summary>
    /// The label used to reference this data in the assembly output.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The raw byte contents.
    /// </summary>
    public required byte[] Bytes { get; init; }
}
