namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// An IR-level function (a named sequence of three-address instructions).
/// </summary>
public sealed record IrFunction
{
    public required string Name { get; init; }

    /// <summary>
    /// Number of parameters this function accepts.
    /// </summary>
    public required int ParameterCount { get; init; }

    /// <summary>
    /// Number of virtual registers (locals + temporaries) used.
    /// </summary>
    public required int RegisterCount { get; init; }

    /// <summary>
    /// The instruction body.
    /// </summary>
    public required IReadOnlyList<IrInstruction> Instructions { get; init; }

    /// <summary>
    /// Maps virtual register index to its allocated size in bytes.
    /// Only registers requiring more than the default 2 bytes (e.g. arrays, structs) are included.
    /// </summary>
    public IReadOnlyDictionary<int, int> RegisterSizes { get; init; } = new Dictionary<int, int>();
}
