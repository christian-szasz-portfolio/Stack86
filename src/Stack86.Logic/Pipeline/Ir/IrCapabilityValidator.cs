namespace Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Safety-net IR validator that checks the generated IR program for constructs
/// the 8086 code generator cannot handle. Catches immediate values outside the
/// 16-bit range and labels that reference undefined targets.
/// </summary>
public sealed class IrCapabilityValidator(ITargetCapabilityProfile profile) : IIrValidator
{
    /// <inheritdoc />
    public IReadOnlyList<IrDiagnostic> Validate(IrProgram program)
    {
        var diagnostics = new List<IrDiagnostic>();
        var definedLabels = CollectDefinedLabels(program);

        foreach (var function in program.Functions)
        {
            foreach (var instruction in function.Instructions)
            {
                this.ValidateOperand(instruction.Left, instruction, definedLabels, diagnostics);
                this.ValidateOperand(instruction.Right, instruction, definedLabels, diagnostics);
                this.ValidateOperand(instruction.Dest, instruction, definedLabels, diagnostics);
            }
        }

        return diagnostics;
    }

    private static HashSet<string> CollectDefinedLabels(IrProgram program)
    {
        var labels = new HashSet<string>(StringComparer.Ordinal);

        foreach (var function in program.Functions)
        {
            labels.Add(function.Name);

            foreach (var instruction in function.Instructions)
            {
                if (instruction.OpCode == IrOpCode.Label && instruction.Left?.Label is { } label)
                {
                    labels.Add(label);
                }
            }
        }

        foreach (var global in program.Globals)
        {
            labels.Add(global.Label);
        }

        return labels;
    }

    private void ValidateOperand(
        IrOperand? operand,
        IrInstruction instruction,
        HashSet<string> definedLabels,
        List<IrDiagnostic> diagnostics)
    {
        if (operand is null)
        {
            return;
        }

        if (operand.Kind == IrOperandKind.Immediate)
        {
            if (operand.Value > profile.MaxIntegerValue || operand.Value < profile.MinIntegerValue)
            {
                diagnostics.Add(new IrDiagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = $"Immediate value {operand.Value} is outside the {profile.TargetName} 16-bit range ({profile.MinIntegerValue}..{profile.MaxIntegerValue}). Value will be truncated.",
                    Line = instruction.SourceLine,
                    Column = 0,
                });
            }
        }

        if (operand.Kind == IrOperandKind.Label && operand.Label is { } targetLabel)
        {
            // Only check jump/call targets — not label definitions themselves
            if (instruction.OpCode is IrOpCode.Jump or IrOpCode.JumpIfZero or IrOpCode.JumpIfNotZero
                or IrOpCode.Call or IrOpCode.CallIndirect or IrOpCode.LoadFunctionAddress)
            {
                if (!definedLabels.Contains(targetLabel))
                {
                    diagnostics.Add(new IrDiagnostic
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Message = $"Label '{targetLabel}' is referenced but not defined.",
                        Line = instruction.SourceLine,
                        Column = 0,
                    });
                }
            }
        }
    }
}
