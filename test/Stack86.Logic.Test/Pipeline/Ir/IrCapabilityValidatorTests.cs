namespace Stack86.Logic.Test.Pipeline.Ir;

using Stack86.Logic.Pipeline.Ir;
using Stack86.Logic.Pipeline.X86Conversion;

[TestClass]
public sealed class IrCapabilityValidatorTests
{
    [TestMethod]
    public void Validate_ImmediateInRange_NoDiagnostics()
    {
        var validator = new IrCapabilityValidator(new X8086CapabilityProfile());
        var program = MakeProgram(IrOpCode.LoadImm, IrOperand.Reg(0), IrOperand.Imm(100));
        Assert.HasCount(0, validator.Validate(program));
    }

    [TestMethod]
    public void Validate_ImmediateOverflow_EmitsWarning()
    {
        var validator = new IrCapabilityValidator(new X8086CapabilityProfile());
        var program = MakeProgram(IrOpCode.LoadImm, IrOperand.Reg(0), IrOperand.Imm(100000));
        var diags = validator.Validate(program);
        Assert.HasCount(1, diags);
        Assert.AreEqual(DiagnosticSeverity.Warning, diags[0].Severity);
    }

    [TestMethod]
    public void Validate_ImmediateUnderflow_EmitsWarning()
    {
        var validator = new IrCapabilityValidator(new X8086CapabilityProfile());
        var program = MakeProgram(IrOpCode.LoadImm, IrOperand.Reg(0), IrOperand.Imm(-100000));
        Assert.HasCount(1, validator.Validate(program));
    }

    [TestMethod]
    public void Validate_UndefinedJumpLabel_EmitsWarning()
    {
        var validator = new IrCapabilityValidator(new X8086CapabilityProfile());
        var program = MakeProgram(IrOpCode.Jump, null, IrOperand.Lbl("missing"));
        var diags = validator.Validate(program);
        Assert.HasCount(1, diags);
        StringAssert.Contains(diags[0].Message, "missing");
    }

    [TestMethod]
    public void Validate_DefinedLabel_NoDiagnostics()
    {
        var validator = new IrCapabilityValidator(new X8086CapabilityProfile());
        var fn = new IrFunction
        {
            Name = "main",
            ParameterCount = 0,
            RegisterCount = 0,
            Instructions =
            [
                new IrInstruction { OpCode = IrOpCode.Label, Left = IrOperand.Lbl("L1") },
                new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl("L1") },
            ],
        };
        Assert.HasCount(0, validator.Validate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    public void Validate_FunctionNameAsCallTarget_NoDiagnostics()
    {
        var validator = new IrCapabilityValidator(new X8086CapabilityProfile());
        var caller = new IrFunction
        {
            Name = "main",
            ParameterCount = 0,
            RegisterCount = 0,
            Instructions = [new IrInstruction { OpCode = IrOpCode.Call, Left = IrOperand.Lbl("foo") }],
        };
        var callee = new IrFunction
        {
            Name = "foo",
            ParameterCount = 0,
            RegisterCount = 0,
            Instructions = [],
        };
        Assert.HasCount(0, validator.Validate(new IrProgram { Functions = [caller, callee] }));
    }

    [TestMethod]
    public void Validate_GlobalLabelReferenced_NoDiagnostics()
    {
        var validator = new IrCapabilityValidator(new X8086CapabilityProfile());
        var fn = new IrFunction
        {
            Name = "main",
            ParameterCount = 0,
            RegisterCount = 0,
            Instructions = [new IrInstruction { OpCode = IrOpCode.Call, Left = IrOperand.Lbl("g") }],
        };
        var program = new IrProgram
        {
            Functions = [fn],
            Globals = [new IrGlobalData { Label = "g", Bytes = [1] }],
        };
        Assert.HasCount(0, validator.Validate(program));
    }

    private static IrProgram MakeProgram(IrOpCode op, IrOperand? dest, IrOperand? left)
    {
        var fn = new IrFunction
        {
            Name = "main",
            ParameterCount = 0,
            RegisterCount = 1,
            Instructions = [new IrInstruction { OpCode = op, Dest = dest, Left = left }],
        };
        return new IrProgram { Functions = [fn] };
    }
}
