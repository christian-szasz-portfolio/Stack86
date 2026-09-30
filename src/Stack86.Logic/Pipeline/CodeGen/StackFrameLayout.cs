namespace Stack86.Logic.Pipeline.CodeGen;

/// <summary>
/// Describes the stack frame layout for a single function.
/// </summary>
public sealed class StackFrameLayout(int parameterCount)
{
    private readonly Dictionary<int, int> registerOffsets = [];
    private readonly Dictionary<int, int> registerSizes = [];
    private int nextOffset;

    public int ParameterCount { get; } = parameterCount;

    /// <summary>
    /// Total bytes of local storage required on the stack.
    /// </summary>
    public int LocalsSize => this.nextOffset;

    /// <summary>
    /// Returns the BP-relative offset for a function parameter (above return address).
    /// Parameter 0 = [BP+4], Parameter 1 = [BP+6], etc.
    /// </summary>
    public static int GetParameterOffset(int parameterIndex)
    {
        return 4 + (parameterIndex * 2);
    }

    /// <summary>
    /// Allocates stack space for a virtual register and returns the [BP-offset].
    /// </summary>
    public int AllocateRegister(int register)
    {
        return this.AllocateVariable(register, 2);
    }

    /// <summary>
    /// Allocates stack space of the specified <paramref name="sizeInBytes"/> for a virtual register.
    /// Returns the BP-relative offset of the start of the allocation.
    /// </summary>
    public int AllocateVariable(int register, int sizeInBytes)
    {
        if (!this.registerOffsets.TryGetValue(register, out var existing))
        {
            this.nextOffset += sizeInBytes;
            this.registerOffsets[register] = this.nextOffset;
            this.registerSizes[register] = sizeInBytes;
            return this.nextOffset;
        }

        return existing;
    }

    /// <summary>
    /// Returns the BP-relative offset for a previously-allocated register.
    /// </summary>
    public int GetOffset(int register)
    {
        return this.registerOffsets[register];
    }

    /// <summary>
    /// Returns the allocated size in bytes for a register, or 2 if not explicitly tracked.
    /// </summary>
    public int GetSize(int register)
    {
        return this.registerSizes.TryGetValue(register, out var size) ? size : 2;
    }

    /// <summary>
    /// Returns true if the register has already been allocated.
    /// </summary>
    public bool IsAllocated(int register)
    {
        return this.registerOffsets.ContainsKey(register);
    }
}
