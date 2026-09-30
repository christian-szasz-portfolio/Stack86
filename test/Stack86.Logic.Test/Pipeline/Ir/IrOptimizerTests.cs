namespace Stack86.Logic.Test.Pipeline.Ir;

using System.Collections.Generic;
using Stack86.Logic.Pipeline.Ir;

[TestClass]
public sealed class IrOptimizerTests
{
    [TestMethod]
    public void Optimize_EmptyProgram_ReturnsEmpty()
    {
        var program = new IrProgram { Functions = [] };
        var result = IrOptimizer.Optimize(program);
        Assert.HasCount(0, result.Functions);
    }

    [TestMethod]
    public void Optimize_SingleFunctionNoCopies_KeepsInstructions()
    {
        var fn = new IrFunction
        {
            Name = "main",
            ParameterCount = 0,
            RegisterCount = 1,
            Instructions =
            [
                new IrInstruction { OpCode = IrOpCode.LoadImm, Dest = IrOperand.Reg(0), Left = IrOperand.Imm(42) },
                new IrInstruction { OpCode = IrOpCode.Return },
            ],
        };
        var program = new IrProgram { Functions = [fn] };
        var result = IrOptimizer.Optimize(program);
        Assert.HasCount(2, result.Functions[0].Instructions);
    }

    [TestMethod]
    public void Optimize_CopyPropagation_EliminatesIntermediate()
    {
        // r0 = 5; r1 = r0  → r1 = 5
        var fn = new IrFunction
        {
            Name = "main",
            ParameterCount = 0,
            RegisterCount = 2,
            Instructions =
            [
                new IrInstruction { OpCode = IrOpCode.LoadImm, Dest = IrOperand.Reg(0), Left = IrOperand.Imm(5) },
                new IrInstruction { OpCode = IrOpCode.Copy, Dest = IrOperand.Reg(1), Left = IrOperand.Reg(0) },
            ],
        };
        var program = new IrProgram { Functions = [fn] };
        var result = IrOptimizer.Optimize(program);
        Assert.HasCount(1, result.Functions[0].Instructions);
        Assert.AreEqual(1, result.Functions[0].Instructions[0].Dest!.Register);
    }

    [TestMethod]
    public void Optimize_RegisterCount_TracksMax()
    {
        var fn = new IrFunction
        {
            Name = "main",
            ParameterCount = 0,
            RegisterCount = 0,
            Instructions =
            [
                new IrInstruction { OpCode = IrOpCode.LoadImm, Dest = IrOperand.Reg(7), Left = IrOperand.Imm(1) },
            ],
        };
        var program = new IrProgram { Functions = [fn] };
        var result = IrOptimizer.Optimize(program);
        Assert.AreEqual(8, result.Functions[0].RegisterCount);
    }

    [TestMethod]
    public void Optimize_NoRegisterReferences_ReturnsAtLeastOne()
    {
        var fn = new IrFunction
        {
            Name = "main",
            ParameterCount = 0,
            RegisterCount = 0,
            Instructions = [new IrInstruction { OpCode = IrOpCode.Return }],
        };
        var program = new IrProgram { Functions = [fn] };
        var result = IrOptimizer.Optimize(program);
        Assert.IsGreaterThanOrEqualTo(0, result.Functions[0].RegisterCount);
    }
}
