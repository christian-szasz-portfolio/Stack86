namespace Stack86.Logic.Test.Pipeline.CodeGen;

using Stack86.Logic.Pipeline.CodeGen;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Direct unit tests for <see cref="Asm8086Generator"/> covering all opcode emit
/// paths via hand-built <see cref="IrProgram"/> inputs.
/// </summary>
[TestClass]
public sealed class Asm8086GeneratorTests
{
    [TestMethod]
    public void Generate_EmptyProgram_EmitsModelStackCode()
    {
        var asm = Generate(new IrProgram { Functions = [] });
        Assert.Contains(".MODEL SMALL", asm);
        Assert.Contains(".STACK", asm);
        Assert.Contains(".CODE", asm);
    }

    [TestMethod]
    public void Generate_ProgramWithoutMain_EmitsFirstFunctionAsEntry()
    {
        var fn1 = MakeFunction("foo", [Ret()]);
        var fn2 = MakeFunction("bar", [Ret()]);
        var asm = Generate(new IrProgram { Functions = [fn1, fn2] });

        Assert.Contains("foo:", asm);
        Assert.Contains("bar PROC", asm);
        Assert.Contains("bar ENDP", asm);
        Assert.Contains("MOV AX, @DATA", asm);
    }

    [TestMethod]
    public void Generate_ProgramWithMain_PutsMainFirstAndEmitsOthers()
    {
        var main = MakeFunction("main", [Ret()]);
        var helper = MakeFunction("helper", [Ret()]);
        var asm = Generate(new IrProgram { Functions = [helper, main] });

        var mainIdx = asm.IndexOf("main:", System.StringComparison.Ordinal);
        var helperIdx = asm.IndexOf("helper PROC", System.StringComparison.Ordinal);
        Assert.IsGreaterThan(-1, mainIdx);
        Assert.IsGreaterThan(-1, helperIdx);
        Assert.IsLessThan(helperIdx, mainIdx);
    }

    [TestMethod]
    public void Generate_FunctionWithLocals_EmitsSubSp()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.LoadImm, Dest = IrOperand.Reg(0), Left = IrOperand.Imm(5) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("SUB SP,", asm);
        Assert.Contains("MOV SP, BP", asm);
    }

    [TestMethod]
    public void Generate_FunctionWithoutTrailingReturn_StillEmitsEpilogue()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Nop },
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("HLT", asm);
        Assert.Contains("NOP", asm);
    }

    [TestMethod]
    [DataRow(IrOpCode.Add, "ADD AX, BX")]
    [DataRow(IrOpCode.Sub, "SUB AX, BX")]
    [DataRow(IrOpCode.And, "AND AX, BX")]
    [DataRow(IrOpCode.Or, "OR AX, BX")]
    [DataRow(IrOpCode.Xor, "XOR AX, BX")]
    public void Generate_BinaryOps_EmitMnemonic(IrOpCode op, string expected)
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = op, Dest = IrOperand.Reg(2), Left = IrOperand.Reg(0), Right = IrOperand.Reg(1) },
            Ret(),
        ]);
        Assert.Contains(expected, Generate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    public void Generate_Mul_EmitsMul()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Mul, Dest = IrOperand.Reg(2), Left = IrOperand.Reg(0), Right = IrOperand.Reg(1) },
            Ret(),
        ]);
        Assert.Contains("MUL BX", Generate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    public void Generate_Div_EmitsDivAndKeepsAx()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Div, Dest = IrOperand.Reg(2), Left = IrOperand.Reg(0), Right = IrOperand.Reg(1) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("DIV BX", asm);
        Assert.Contains("XOR DX, DX", asm);
    }

    [TestMethod]
    public void Generate_Mod_EmitsDivThenMovDx()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Mod, Dest = IrOperand.Reg(2), Left = IrOperand.Reg(0), Right = IrOperand.Reg(1) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("DIV BX", asm);
        Assert.Contains("MOV AX, DX", asm);
    }

    [TestMethod]
    public void Generate_Neg_EmitsNeg()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Neg, Dest = IrOperand.Reg(1), Left = IrOperand.Reg(0) },
            Ret(),
        ]);
        Assert.Contains("NEG AX", Generate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    public void Generate_Not_EmitsNot()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Not, Dest = IrOperand.Reg(1), Left = IrOperand.Reg(0) },
            Ret(),
        ]);
        Assert.Contains("NOT AX", Generate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    [DataRow(IrOpCode.Shl, "SHL AX, CL")]
    [DataRow(IrOpCode.Shr, "SHR AX, CL")]
    public void Generate_Shifts_EmitShlOrShr(IrOpCode op, string expected)
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = op, Dest = IrOperand.Reg(2), Left = IrOperand.Reg(0), Right = IrOperand.Reg(1) },
            Ret(),
        ]);
        Assert.Contains(expected, Generate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    [DataRow(IrOpCode.CmpEq, "JE")]
    [DataRow(IrOpCode.CmpNe, "JNE")]
    [DataRow(IrOpCode.CmpLt, "JL ")]
    [DataRow(IrOpCode.CmpLe, "JLE")]
    [DataRow(IrOpCode.CmpGt, "JG ")]
    [DataRow(IrOpCode.CmpGe, "JGE")]
    public void Generate_CompareOps_EmitJumpMnemonic(IrOpCode op, string expected)
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = op, Dest = IrOperand.Reg(2), Left = IrOperand.Reg(0), Right = IrOperand.Reg(1) },
            Ret(),
        ]);
        Assert.Contains(expected, Generate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    public void Generate_Jump_EmitsJmp()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Label, Left = IrOperand.Lbl("L1") },
            new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl("L1") },
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("JMP L1", asm);
        Assert.Contains("L1:", asm);
    }

    [TestMethod]
    public void Generate_JumpIfZero_EmitsCmpAndJe()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.JumpIfZero, Left = IrOperand.Reg(0), Right = IrOperand.Lbl("L1") },
            new IrInstruction { OpCode = IrOpCode.Label, Left = IrOperand.Lbl("L1") },
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("JE L1", asm);
    }

    [TestMethod]
    public void Generate_JumpIfNotZero_EmitsCmpAndJne()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.JumpIfNotZero, Left = IrOperand.Reg(0), Right = IrOperand.Lbl("L1") },
            new IrInstruction { OpCode = IrOpCode.Label, Left = IrOperand.Lbl("L1") },
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("JNE L1", asm);
    }

    [TestMethod]
    public void Generate_CallWithArgs_EmitsCallAndCleansStack()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Push, Left = IrOperand.Imm(1) },
            new IrInstruction { OpCode = IrOpCode.Push, Left = IrOperand.Imm(2) },
            new IrInstruction { OpCode = IrOpCode.Call, Dest = IrOperand.Reg(0), Left = IrOperand.Lbl("foo"), Right = IrOperand.Imm(2) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("CALL foo", asm);
        Assert.Contains("ADD SP, 4", asm);
    }

    [TestMethod]
    public void Generate_CallIndirect_EmitsCallAx()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Push, Left = IrOperand.Imm(1) },
            new IrInstruction { OpCode = IrOpCode.CallIndirect, Dest = IrOperand.Reg(0), Left = IrOperand.Reg(1), Right = IrOperand.Imm(1) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("CALL AX", asm);
        Assert.Contains("ADD SP, 2", asm);
    }

    [TestMethod]
    public void Generate_Push_LoadsOperandAndPushesAx()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Push, Left = IrOperand.Imm(42) },
            Ret(),
        ]);
        Assert.Contains("PUSH AX", Generate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    public void Generate_PopWithDest_StoresAx()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Pop, Dest = IrOperand.Reg(0) },
            Ret(),
        ]);
        Assert.Contains("POP AX", Generate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    public void Generate_LoadMem_EmitsMovBxAndIndirect()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.LoadMem, Dest = IrOperand.Reg(1), Left = IrOperand.Reg(0) },
            Ret(),
        ]);
        Assert.Contains("MOV AX, [BX]", Generate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    public void Generate_StoreMem_EmitsMovIndirect()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.StoreMem, Dest = IrOperand.Reg(0), Left = IrOperand.Reg(1) },
            Ret(),
        ]);
        Assert.Contains("MOV [BX], AX", Generate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    public void Generate_LoadMem8_EmitsXorAxAndMovAl()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.LoadMem8, Dest = IrOperand.Reg(1), Left = IrOperand.Reg(0) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("XOR AX, AX", asm);
        Assert.Contains("MOV AL, [BX]", asm);
    }

    [TestMethod]
    public void Generate_StoreMem8_EmitsMovAlIndirect()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.StoreMem8, Dest = IrOperand.Reg(0), Left = IrOperand.Reg(1) },
            Ret(),
        ]);
        Assert.Contains("MOV [BX], AL", Generate(new IrProgram { Functions = [fn] }));
    }

    [TestMethod]
    public void Generate_BlockCopy_EmitsRepMovsb()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.BlockCopy, Dest = IrOperand.Reg(0), Left = IrOperand.Reg(1), Right = IrOperand.Imm(8) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("REP MOVSB", asm);
        Assert.Contains("MOV CX, 8", asm);
    }

    [TestMethod]
    public void Generate_LoadAddress_EmitsAddOrSub()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.LoadAddress, Dest = IrOperand.Reg(1), Left = IrOperand.Reg(0) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("MOV AX, BP", asm);
    }

    [TestMethod]
    public void Generate_PrintChar_EmitsInt21h()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.PrintChar, Left = IrOperand.Imm(65) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("MOV AH, 2", asm);
        Assert.Contains("INT 21h", asm);
    }

    [TestMethod]
    public void Generate_PrintStr_WithLabel_EmitsMovDxLabel()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.PrintStr, Left = IrOperand.Lbl("msg") },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("MOV DX, msg", asm);
        Assert.Contains("MOV AH, 9", asm);
    }

    [TestMethod]
    public void Generate_PrintStr_WithRegister_LoadsThenMov()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.PrintStr, Left = IrOperand.Reg(0) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("MOV DX, AX", asm);
        Assert.Contains("MOV AH, 9", asm);
    }

    [TestMethod]
    public void Generate_PrintInt_EmitsCallPrintIntAndHelper()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.PrintInt, Left = IrOperand.Imm(123) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("CALL _print_int", asm);
        Assert.Contains("_print_int PROC", asm);
        Assert.Contains("_print_int ENDP", asm);
    }

    [TestMethod]
    public void Generate_NoPrintInt_DoesNotEmitHelper()
    {
        var fn = MakeMain([Ret()]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.IsFalse(asm.Contains("_print_int PROC", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void Generate_Syscall_EmitsInt86h()
    {
        var fn = MakeMain([
            new IrInstruction { OpCode = IrOpCode.Push, Left = IrOperand.Imm(1) },
            new IrInstruction { OpCode = IrOpCode.Syscall, Dest = IrOperand.Reg(0), Left = IrOperand.Imm(7), Right = IrOperand.Imm(1) },
            Ret(),
        ]);
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("MOV AH, 7", asm);
        Assert.Contains("INT 86h", asm);
        Assert.Contains("ADD SP, 2", asm);
    }

    [TestMethod]
    public void Generate_StructDefinitions_EmitStrucEnds()
    {
        var program = new IrProgram
        {
            Functions = [MakeMain([Ret()])],
            StructDefinitions =
            [
                new IrStructDefinition("Point", [
                    new IrStructField("x", 2),
                    new IrStructField("y", 2),
                    new IrStructField("flag", 1),
                ]),
            ],
        };
        var asm = Generate(program);
        Assert.Contains("Point STRUC", asm);
        Assert.Contains("x DW ?", asm);
        Assert.Contains("flag DB ?", asm);
        Assert.Contains("Point ENDS", asm);
    }

    [TestMethod]
    public void Generate_Globals_EmitsDataSegment()
    {
        var program = new IrProgram
        {
            Functions = [MakeMain([Ret()])],
            Globals = [new IrGlobalData { Label = "msg", Bytes = [(byte)'h', (byte)'i', 0] }],
        };
        var asm = Generate(program);
        Assert.Contains(".DATA", asm);
        Assert.Contains("msg:", asm);
    }

    [TestMethod]
    public void Generate_Vtables_EmitsDwMethodLabels()
    {
        var program = new IrProgram
        {
            Functions = [MakeMain([Ret()])],
            Vtables = [new IrVtableEntry("vt_Foo", ["Foo_a", "Foo_b"])],
        };
        var asm = Generate(program);
        Assert.Contains("vt_Foo:", asm);
        Assert.Contains("Foo_a", asm);
        Assert.Contains("Foo_b", asm);
    }

    [TestMethod]
    public void Generate_FunctionWithParameter_EmitsAccessAtBpPlus4()
    {
        var fn = new IrFunction
        {
            Name = "main",
            ParameterCount = 1,
            RegisterCount = 1,
            Instructions =
            [
                new IrInstruction { OpCode = IrOpCode.Copy, Dest = IrOperand.Reg(0), Left = IrOperand.Reg(0) },
                Ret(),
            ],
        };
        var asm = Generate(new IrProgram { Functions = [fn] });
        Assert.Contains("[BP+4]", asm);
    }

    private static string Generate(IrProgram program) =>
        new Asm8086Generator().Generate(program);

    private static IrFunction MakeMain(System.Collections.Generic.List<IrInstruction> instructions) =>
        MakeFunction("main", instructions);

    private static IrFunction MakeFunction(string name, System.Collections.Generic.List<IrInstruction> instructions) =>
        new()
        {
            Name = name,
            ParameterCount = 0,
            RegisterCount = 8,
            Instructions = instructions,
        };

    private static IrInstruction Ret() => new() { OpCode = IrOpCode.Return };
}
