namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Peephole optimisation pass over the IR.
/// Currently performs copy-propagation to eliminate redundant temporaries.
/// </summary>
public sealed class IrOptimizer
{
    /// <summary>
    /// Returns a new <see cref="IrProgram"/> with optimised functions.
    /// </summary>
    public static IrProgram Optimize(IrProgram program)
    {
        var optimizedFunctions = new List<IrFunction>(program.Functions.Count);

        foreach (var function in program.Functions)
        {
            optimizedFunctions.Add(OptimizeFunction(function));
        }

        return program with { Functions = optimizedFunctions };
    }

    private static IrFunction OptimizeFunction(IrFunction function)
    {
        var instructions = new List<IrInstruction>(function.Instructions);

        var changed = true;
        while (changed)
        {
            changed = false;
            var useCounts = BuildUseCounts(instructions);

            for (var i = 0; i < instructions.Count - 1; i++)
            {
                var producer = instructions[i];
                var consumer = instructions[i + 1];

                if (!IsCopyPropagationCandidate(producer, consumer, useCounts))
                {
                    continue;
                }

                // Retarget the producer's dest to the copy's dest, eliminating the copy.
                instructions[i] = producer with { Dest = consumer.Dest };
                instructions.RemoveAt(i + 1);
                changed = true;
                break; // Rebuild use-counts after mutation.
            }
        }

        // Determine actual register count after optimisation.
        var maxRegister = DetermineMaxRegister(instructions, function.ParameterCount);

        return function with
        {
            Instructions = instructions,
            RegisterCount = maxRegister + 1,
        };
    }

    /// <summary>
    /// Returns true when <paramref name="producer"/> writes to a temporary that
    /// is immediately consumed only by the <paramref name="consumer"/> Copy.
    /// </summary>
    private static bool IsCopyPropagationCandidate(
        IrInstruction producer,
        IrInstruction consumer,
        Dictionary<int, int> useCounts)
    {
        // Consumer must be a Copy from a register.
        if (consumer.OpCode != IrOpCode.Copy)
        {
            return false;
        }

        if (consumer.Left?.Kind != IrOperandKind.Register)
        {
            return false;
        }

        // Producer must write to the same register the Copy reads.
        if (producer.Dest?.Kind != IrOperandKind.Register)
        {
            return false;
        }

        var tempReg = producer.Dest.Register;
        if (consumer.Left.Register != tempReg)
        {
            return false;
        }

        // The temp register must appear exactly twice: once as Dest in producer, once as Left in consumer.
        return useCounts.TryGetValue(tempReg, out var count) && count == 2;
    }

    /// <summary>
    /// Counts the total number of operand references per register across all instructions.
    /// </summary>
    private static Dictionary<int, int> BuildUseCounts(List<IrInstruction> instructions)
    {
        var counts = new Dictionary<int, int>();

        foreach (var instr in instructions)
        {
            CountOperand(counts, instr.Dest);
            CountOperand(counts, instr.Left);
            CountOperand(counts, instr.Right);
        }

        return counts;
    }

    private static void CountOperand(Dictionary<int, int> counts, IrOperand? operand)
    {
        if (operand?.Kind == IrOperandKind.Register)
        {
            counts[operand.Register] = counts.GetValueOrDefault(operand.Register) + 1;
        }
    }

    /// <summary>
    /// Finds the highest register index referenced by any instruction so the
    /// code generator can size the stack frame correctly.
    /// </summary>
    private static int DetermineMaxRegister(List<IrInstruction> instructions, int parameterCount)
    {
        var max = parameterCount - 1;

        foreach (var instr in instructions)
        {
            max = MaxReg(max, instr.Dest);
            max = MaxReg(max, instr.Left);
            max = MaxReg(max, instr.Right);
        }

        return max < 0 ? 0 : max;
    }

    private static int MaxReg(int current, IrOperand? operand)
    {
        if (operand?.Kind == IrOperandKind.Register && operand.Register > current)
        {
            return operand.Register;
        }

        return current;
    }
}
