namespace Stack86.Logic.Pipeline.CodeGen;

using System.Text;
using Stack86.Common.Exceptions;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Generates 8086 assembly from the IR representation.
/// Targets the Stack86 emulator's instruction set.
/// </summary>
public sealed class Asm8086Generator : ICodeGenerator
{
    public string Generate(IrProgram program)
    {
        var sb = new StringBuilder();

        EmitStructDefinitions(sb, program);

        sb.AppendLine(".MODEL SMALL");
        sb.Append(".STACK ");
        sb.AppendLine(ComputeStackSize(program).ToString());
        EmitDataSegment(sb, program);
        sb.AppendLine(".CODE");
        EmitCodeSegment(sb, program);

        return sb.ToString();
    }

    /// <summary>
    /// Computes the total stack size needed by the program.
    /// Each function uses: 2 (saved BP) + 2 (return address) + locals + max parameter push.
    /// For nested calls the worst case is the sum down the deepest call chain,
    /// but a safe conservative bound is the sum of all frames + a safety margin.
    /// Rounds up to the nearest 16-byte boundary.
    /// </summary>
    private static int ComputeStackSize(IrProgram program)
    {
        var total = 0;

        foreach (var func in program.Functions)
        {
            var frame = BuildFrame(func);

            // BP save (2) + return address (2) + locals
            var frameSize = 4 + frame.LocalsSize;

            // Account for parameter pushes (worst case: the max arguments pushed for any call)
            var maxArgs = 0;
            foreach (var instr in func.Instructions)
            {
                if ((instr.OpCode == IrOpCode.Call || instr.OpCode == IrOpCode.CallIndirect || instr.OpCode == IrOpCode.Syscall) && instr.Right is not null)
                {
                    var argCount = instr.Right.Value;
                    if (argCount > maxArgs)
                    {
                        maxArgs = argCount;
                    }
                }
            }

            frameSize += maxArgs * 2;
            total += frameSize;
        }

        // Round up to nearest 16-byte boundary, minimum 256
        total = Math.Max(total * 2, 256); // 2x headroom for recursion safety
        return (total + 15) & ~15;
    }

    private static StackFrameLayout BuildFrame(IrFunction func)
    {
        var frame = new StackFrameLayout(func.ParameterCount);
        var usedRegisters = new HashSet<int>();

        foreach (var instr in func.Instructions)
        {
            CollectRegisters(usedRegisters, instr);
        }

        foreach (var r in usedRegisters.Order())
        {
            if (r >= func.ParameterCount)
            {
                frame.AllocateRegister(r);
            }
        }

        return frame;
    }

    private static void EmitStructDefinitions(StringBuilder sb, IrProgram program)
    {
        if (program.StructDefinitions.Count == 0)
        {
            return;
        }

        foreach (var structDef in program.StructDefinitions)
        {
            sb.Append(structDef.Name);
            sb.AppendLine(" STRUC");

            foreach (var field in structDef.Fields)
            {
                sb.Append("  ");
                sb.Append(field.Name);
                sb.Append(field.SizeInBytes == 1 ? " DB ?" : " DW ?");
                sb.AppendLine();
            }

            sb.Append(structDef.Name);
            sb.AppendLine(" ENDS");
        }

        sb.AppendLine();
    }

    private static void EmitDataSegment(StringBuilder sb, IrProgram program)
    {
        if (program.Globals.Count == 0 && program.Vtables.Count == 0)
        {
            return;
        }

        sb.AppendLine(".DATA");

        foreach (var global in program.Globals)
        {
            sb.Append(global.Label);
            sb.Append(": DB ");
            EmitDbBytes(sb, global.Bytes);
            sb.AppendLine();
        }

        // Emit vtables: DW method1, method2, ...
        foreach (var vtable in program.Vtables)
        {
            sb.Append(vtable.Label);
            sb.Append(": DW ");

            for (var i = 0; i < vtable.MethodLabels.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(vtable.MethodLabels[i]);
            }

            sb.AppendLine();
        }
    }

    /// <summary>
    /// Emits DB operands as readable character literals where possible.
    /// Printable ASCII characters are grouped into quoted strings; non-printable bytes
    /// are emitted as decimal numbers — following MASM convention.
    /// </summary>
    private static void EmitDbBytes(StringBuilder sb, byte[] bytes)
    {
        var first = true;

        var i = 0;
        while (i < bytes.Length)
        {
            if (IsPrintableAscii(bytes[i]))
            {
                // Collect a run of printable characters
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append('\'');
                while (i < bytes.Length && IsPrintableAscii(bytes[i]))
                {
                    sb.Append((char)bytes[i]);
                    i++;
                }

                sb.Append('\'');
                first = false;
            }
            else
            {
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append(bytes[i]);
                i++;
                first = false;
            }
        }
    }

    private static bool IsPrintableAscii(byte b)
    {
        return b >= 0x20 && b <= 0x7E;
    }

    private static void EmitCodeSegment(StringBuilder sb, IrProgram program)
    {
        // Emit main first so execution starts at the top — no JMP needed.
        var mainIndex = -1;
        for (var i = 0; i < program.Functions.Count; i++)
        {
            if (program.Functions[i].Name == "main")
            {
                mainIndex = i;
                break;
            }
        }

        if (mainIndex >= 0)
        {
            EmitFunction(sb, program.Functions[mainIndex], isEntryPoint: true);

            for (var i = 0; i < program.Functions.Count; i++)
            {
                if (i == mainIndex)
                {
                    continue;
                }

                sb.AppendLine();
                EmitFunction(sb, program.Functions[i], isEntryPoint: false);
            }
        }
        else
        {
            for (var i = 0; i < program.Functions.Count; i++)
            {
                EmitFunction(sb, program.Functions[i], isEntryPoint: i == 0);

                if (i < program.Functions.Count - 1)
                {
                    sb.AppendLine();
                }
            }
        }

        // Emit the _print_int helper if any function uses PrintInt.
        if (NeedsPrintIntHelper(program))
        {
            sb.AppendLine();
            EmitPrintIntHelper(sb);
        }
    }

    private static void EmitFunction(StringBuilder sb, IrFunction function, bool isEntryPoint)
    {
        var frame = new StackFrameLayout(function.ParameterCount);

        // Allocate only registers that actually appear in instructions
        var usedRegisters = new HashSet<int>();
        foreach (var instr in function.Instructions)
        {
            CollectRegisters(usedRegisters, instr);
        }

        foreach (var r in usedRegisters.Order())
        {
            if (r >= function.ParameterCount)
            {
                var size = function.RegisterSizes.GetValueOrDefault(r, 2);
                frame.AllocateVariable(r, size);
            }
        }

        // Function label
        sb.Append(function.Name);
        sb.AppendLine(isEntryPoint ? ":" : " PROC");

        // Prologue — entry point MUST initialise DS from @DATA
        if (isEntryPoint)
        {
            sb.AppendLine("  MOV AX, @DATA");
            sb.AppendLine("  MOV DS, AX");
        }

        sb.AppendLine("  PUSH BP");
        sb.AppendLine("  MOV BP, SP");

        if (frame.LocalsSize > 0)
        {
            sb.Append("  SUB SP, ");
            sb.AppendLine(frame.LocalsSize.ToString());
        }

        // Body
        foreach (var instr in function.Instructions)
        {
            EmitInstruction(sb, instr, frame, function, isEntryPoint);
        }

        // Epilogue (reached by fall-through on void functions; skip if last instruction is Return)
        if (function.Instructions.Count == 0 || function.Instructions[^1].OpCode != IrOpCode.Return)
        {
            EmitEpilogue(sb, frame, isEntryPoint);
        }

        if (!isEntryPoint)
        {
            sb.Append(function.Name);
            sb.AppendLine(" ENDP");
        }
    }

    private static void EmitInstruction(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function, bool isEntryPoint)
    {
        switch (instr.OpCode)
        {
            case IrOpCode.LoadImm:
                EmitLoadImm(sb, instr, frame, function);
                break;
            case IrOpCode.Copy:
                EmitCopy(sb, instr, frame, function);
                break;
            case IrOpCode.Add:
            case IrOpCode.Sub:
            case IrOpCode.And:
            case IrOpCode.Or:
            case IrOpCode.Xor:
                EmitBinaryOp(sb, instr, frame, function);
                break;
            case IrOpCode.Mul:
                EmitMul(sb, instr, frame, function);
                break;
            case IrOpCode.Div:
            case IrOpCode.Mod:
                EmitDivMod(sb, instr, frame, function);
                break;
            case IrOpCode.Neg:
                EmitNeg(sb, instr, frame, function);
                break;
            case IrOpCode.Not:
                EmitNot(sb, instr, frame, function);
                break;
            case IrOpCode.Shl:
                EmitShift(sb, "SHL", instr, frame, function);
                break;
            case IrOpCode.Shr:
                EmitShift(sb, "SHR", instr, frame, function);
                break;
            case IrOpCode.CmpEq:
            case IrOpCode.CmpNe:
            case IrOpCode.CmpLt:
            case IrOpCode.CmpLe:
            case IrOpCode.CmpGt:
            case IrOpCode.CmpGe:
                EmitCompare(sb, instr, frame, function);
                break;
            case IrOpCode.Label:
                sb.Append(instr.Left!.Label);
                sb.AppendLine(":");
                break;
            case IrOpCode.Jump:
                sb.Append("  JMP ");
                sb.AppendLine(instr.Left!.Label);
                break;
            case IrOpCode.JumpIfZero:
                LoadOperandToAx(sb, instr.Left!, frame, function);
                sb.AppendLine("  CMP AX, 0");
                sb.Append("  JE ");
                sb.AppendLine(instr.Right!.Label);
                break;
            case IrOpCode.JumpIfNotZero:
                LoadOperandToAx(sb, instr.Left!, frame, function);
                sb.AppendLine("  CMP AX, 0");
                sb.Append("  JNE ");
                sb.AppendLine(instr.Right!.Label);
                break;
            case IrOpCode.Call:
                EmitCall(sb, instr, frame, function);
                break;
            case IrOpCode.CallIndirect:
                EmitCallIndirect(sb, instr, frame, function);
                break;
            case IrOpCode.Return:
                EmitReturn(sb, instr, frame, function, isEntryPoint);
                break;
            case IrOpCode.Push:
                LoadOperandToAx(sb, instr.Left!, frame, function);
                sb.AppendLine("  PUSH AX");
                break;
            case IrOpCode.Pop:
                sb.AppendLine("  POP AX");
                if (instr.Dest != null)
                {
                    StoreAxToOperand(sb, instr.Dest, frame, function);
                }

                break;
            case IrOpCode.LoadMem:
                EmitLoadMem(sb, instr, frame, function);
                break;
            case IrOpCode.StoreMem:
                EmitStoreMem(sb, instr, frame, function);
                break;
            case IrOpCode.LoadMem8:
                EmitLoadMem8(sb, instr, frame, function);
                break;
            case IrOpCode.StoreMem8:
                EmitStoreMem8(sb, instr, frame, function);
                break;
            case IrOpCode.BlockCopy:
                EmitBlockCopy(sb, instr, frame, function);
                break;
            case IrOpCode.LoadAddress:
                EmitLoadAddress(sb, instr, frame, function);
                break;
            case IrOpCode.LoadFunctionAddress:
                EmitLoadFunctionAddress(sb, instr, frame, function);
                break;
            case IrOpCode.PrintChar:
                EmitPrintChar(sb, instr, frame, function);
                break;
            case IrOpCode.PrintStr:
                EmitPrintStr(sb, instr, frame, function);
                break;
            case IrOpCode.PrintInt:
                EmitPrintInt(sb, instr, frame, function);
                break;
            case IrOpCode.Syscall:
                EmitSyscall(sb, instr, frame, function);
                break;
            case IrOpCode.Nop:
                sb.AppendLine("  NOP");
                break;
        }
    }

    // ────────────────────────────────────────────────────────
    //  Emit helpers
    // ────────────────────────────────────────────────────────
    private static void EmitLoadImm(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        sb.Append("  MOV AX, ");
        sb.AppendLine(instr.Left!.Value.ToString());
        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitLoadFunctionAddress(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        // Load the function's code-label address into AX (no OFFSET; bare label
        // resolves to the function's byte address in the emulator).
        sb.Append("  MOV AX, ");
        sb.AppendLine(instr.Left!.Label);
        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitCopy(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        LoadOperandToAx(sb, instr.Left!, frame, function);
        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitBinaryOp(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  PUSH AX");
        LoadOperandToAx(sb, instr.Right!, frame, function);
        sb.AppendLine("  MOV BX, AX");
        sb.AppendLine("  POP AX");

        var mnemonic = instr.OpCode switch
        {
            IrOpCode.Add => "ADD",
            IrOpCode.Sub => "SUB",
            IrOpCode.And => "AND",
            IrOpCode.Or => "OR",
            IrOpCode.Xor => "XOR",
            _ => throw new CompilerInvariantException($"Unexpected binary op: {instr.OpCode}"),
        };

        sb.Append("  ");
        sb.Append(mnemonic);
        sb.AppendLine(" AX, BX");
        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitMul(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  PUSH AX");
        LoadOperandToAx(sb, instr.Right!, frame, function);
        sb.AppendLine("  MOV BX, AX");
        sb.AppendLine("  POP AX");
        sb.AppendLine("  MUL BX");
        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitDivMod(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  PUSH AX");
        LoadOperandToAx(sb, instr.Right!, frame, function);
        sb.AppendLine("  MOV BX, AX");
        sb.AppendLine("  POP AX");
        sb.AppendLine("  XOR DX, DX");
        sb.AppendLine("  DIV BX");

        if (instr.OpCode == IrOpCode.Mod)
        {
            sb.AppendLine("  MOV AX, DX");
        }

        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitNeg(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  NEG AX");
        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitNot(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  NOT AX");
        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitShift(StringBuilder sb, string mnemonic, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        LoadOperandToAx(sb, instr.Right!, frame, function);
        sb.AppendLine("  MOV CX, AX");
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.Append("  ");
        sb.Append(mnemonic);
        sb.AppendLine(" AX, CL");
        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitCompare(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  PUSH AX");
        LoadOperandToAx(sb, instr.Right!, frame, function);
        sb.AppendLine("  MOV BX, AX");
        sb.AppendLine("  POP AX");
        sb.AppendLine("  CMP AX, BX");

        var setLabel = $"_cmp_set_{instr.GetHashCode():X}";
        var endLabel = $"_cmp_end_{instr.GetHashCode():X}";

        var jumpMnemonic = instr.OpCode switch
        {
            IrOpCode.CmpEq => "JE",
            IrOpCode.CmpNe => "JNE",
            IrOpCode.CmpLt => "JL",
            IrOpCode.CmpLe => "JLE",
            IrOpCode.CmpGt => "JG",
            IrOpCode.CmpGe => "JGE",
            _ => throw new CompilerInvariantException("Unexpected branch in Asm8086Generator."),
        };

        sb.Append("  ");
        sb.Append(jumpMnemonic);
        sb.Append(" ");
        sb.AppendLine(setLabel);
        sb.AppendLine("  MOV AX, 0");
        sb.Append("  JMP ");
        sb.AppendLine(endLabel);
        sb.Append(setLabel);
        sb.AppendLine(":");
        sb.AppendLine("  MOV AX, 1");
        sb.Append(endLabel);
        sb.AppendLine(":");
        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitCall(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        // Arguments should already be pushed via Push instructions
        sb.Append("  CALL ");
        sb.AppendLine(instr.Left!.Label);

        // Clean up pushed arguments
        if (instr.Right != null && instr.Right.Value > 0)
        {
            sb.Append("  ADD SP, ");
            sb.AppendLine((instr.Right.Value * 2).ToString());
        }

        // Store return value (in AX) to dest
        if (instr.Dest != null)
        {
            StoreAxToOperand(sb, instr.Dest, frame, function);
        }
    }

    private static void EmitCallIndirect(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        // Left = register holding the function address, Right = arg count
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  CALL AX");

        // Clean up pushed arguments
        if (instr.Right != null && instr.Right.Value > 0)
        {
            sb.Append("  ADD SP, ");
            sb.AppendLine((instr.Right.Value * 2).ToString());
        }

        // Store return value (in AX) to dest
        if (instr.Dest != null)
        {
            StoreAxToOperand(sb, instr.Dest, frame, function);
        }
    }

    private static void EmitReturn(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function, bool isEntryPoint)
    {
        if (instr.Left != null)
        {
            LoadOperandToAx(sb, instr.Left, frame, function);
        }

        EmitEpilogue(sb, frame, isEntryPoint);
    }

    private static void EmitLoadMem(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  MOV BX, AX");
        sb.AppendLine("  MOV AX, [BX]");
        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitStoreMem(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        // Dest = address, Left = value to store
        LoadOperandToAx(sb, instr.Dest!, frame, function);
        sb.AppendLine("  MOV BX, AX");
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  MOV [BX], AX");
    }

    private static void EmitLoadMem8(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        // Load a single byte from [address] into AX (zero-extended)
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  MOV BX, AX");
        sb.AppendLine("  XOR AX, AX");
        sb.AppendLine("  MOV AL, [BX]");
        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitStoreMem8(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        // Store the low byte of value to [address]
        LoadOperandToAx(sb, instr.Dest!, frame, function);
        sb.AppendLine("  MOV BX, AX");
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  MOV [BX], AL");
    }

    private static void EmitBlockCopy(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        // Dest = destination register (address), Left = source register (address), Right = byte count
        // Load destination address into DI
        LoadOperandToAx(sb, instr.Dest!, frame, function);
        sb.AppendLine("  MOV DI, AX");

        // LEA for dest: compute stack address
        sb.AppendLine("  PUSH DI");

        // Load source address into SI
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  MOV SI, AX");

        sb.AppendLine("  POP DI");

        // Set CX = byte count
        sb.Append("  MOV CX, ");
        sb.AppendLine(instr.Right!.Value.ToString());

        // Copy bytes
        sb.AppendLine("  REP MOVSB");
    }

    private static void EmitLoadAddress(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        // Load the BP-relative address of a stack variable into AX
        var srcReg = instr.Left!.Register;
        var offset = GetRegisterBpOffset(srcReg, frame, function);
        sb.AppendLine("  MOV AX, BP");

        if (offset > 0)
        {
            sb.Append("  ADD AX, ");
            sb.AppendLine(offset.ToString());
        }
        else if (offset < 0)
        {
            sb.Append("  SUB AX, ");
            sb.AppendLine((-offset).ToString());
        }

        StoreAxToOperand(sb, instr.Dest!, frame, function);
    }

    private static void EmitEpilogue(StringBuilder sb, StackFrameLayout frame, bool isEntryPoint)
    {
        if (frame.LocalsSize > 0)
        {
            sb.AppendLine("  MOV SP, BP");
        }

        sb.AppendLine("  POP BP");
        sb.AppendLine(isEntryPoint ? "  HLT" : "  RET");
    }

    // ────────────────────────────────────────────────────────
    //  I/O helpers (printf expansion)
    // ────────────────────────────────────────────────────────
    private static void EmitPrintChar(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  MOV DL, AL");
        sb.AppendLine("  MOV AH, 2");
        sb.AppendLine("  INT 21h");
    }

    private static void EmitPrintStr(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        if (instr.Left!.Kind == IrOperandKind.Label)
        {
            sb.Append("  MOV DX, ");
            sb.AppendLine(instr.Left.Label);
        }
        else
        {
            LoadOperandToAx(sb, instr.Left, frame, function);
            sb.AppendLine("  MOV DX, AX");
        }

        sb.AppendLine("  MOV AH, 9");
        sb.AppendLine("  INT 21h");
    }

    private static void EmitPrintInt(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        LoadOperandToAx(sb, instr.Left!, frame, function);
        sb.AppendLine("  PUSH AX");
        sb.AppendLine("  CALL _print_int");
        sb.AppendLine("  ADD SP, 2");
    }

    /// <summary>
    /// Emits a standard library syscall via INT 86h.
    /// Arguments are already pushed onto the stack; AH holds the service number.
    /// </summary>
    private static void EmitSyscall(StringBuilder sb, IrInstruction instr, StackFrameLayout frame, IrFunction function)
    {
        sb.Append("  MOV AH, ");
        sb.AppendLine(instr.Left!.Value.ToString());
        sb.AppendLine("  INT 86h");

        // Clean up pushed arguments
        if (instr.Right != null && instr.Right.Value > 0)
        {
            sb.Append("  ADD SP, ");
            sb.AppendLine((instr.Right.Value * 2).ToString());
        }

        // Store return value (in AX) to dest
        if (instr.Dest != null)
        {
            StoreAxToOperand(sb, instr.Dest, frame, function);
        }
    }

    private static bool NeedsPrintIntHelper(IrProgram program)
    {
        return program.Functions.Any(f => f.Instructions.Any(i => i.OpCode == IrOpCode.PrintInt));
    }

    /// <summary>
    /// Emits a runtime helper that prints a signed 16-bit integer in AX as a decimal string via INT 21h.
    /// </summary>
    private static void EmitPrintIntHelper(StringBuilder sb)
    {
        sb.AppendLine("_print_int PROC");
        sb.AppendLine("  PUSH BP");
        sb.AppendLine("  MOV BP, SP");
        sb.AppendLine("  PUSH BX");
        sb.AppendLine("  PUSH CX");
        sb.AppendLine("  PUSH DX");
        sb.AppendLine("  MOV AX, [BP+4]");
        sb.AppendLine("  CMP AX, 0");
        sb.AppendLine("  JGE _pi_pos");
        sb.AppendLine("  PUSH AX");
        sb.AppendLine("  MOV DL, 45");      // '-'
        sb.AppendLine("  MOV AH, 2");
        sb.AppendLine("  INT 21h");
        sb.AppendLine("  POP AX");
        sb.AppendLine("  NEG AX");
        sb.AppendLine("_pi_pos:");
        sb.AppendLine("  MOV CX, 0");
        sb.AppendLine("  MOV BX, 10");
        sb.AppendLine("_pi_div:");
        sb.AppendLine("  XOR DX, DX");
        sb.AppendLine("  DIV BX");
        sb.AppendLine("  PUSH DX");
        sb.AppendLine("  INC CX");
        sb.AppendLine("  CMP AX, 0");
        sb.AppendLine("  JNE _pi_div");
        sb.AppendLine("_pi_prt:");
        sb.AppendLine("  POP DX");
        sb.AppendLine("  ADD DL, 48");      // + '0'
        sb.AppendLine("  MOV AH, 2");
        sb.AppendLine("  INT 21h");
        sb.AppendLine("  DEC CX");
        sb.AppendLine("  CMP CX, 0");
        sb.AppendLine("  JNE _pi_prt");
        sb.AppendLine("  POP DX");
        sb.AppendLine("  POP CX");
        sb.AppendLine("  POP BX");
        sb.AppendLine("  POP BP");
        sb.AppendLine("  RET");
        sb.AppendLine("_print_int ENDP");
    }

    // ────────────────────────────────────────────────────────
    //  Operand addressing
    // ────────────────────────────────────────────────────────
    private static void LoadOperandToAx(StringBuilder sb, IrOperand operand, StackFrameLayout frame, IrFunction function)
    {
        switch (operand.Kind)
        {
            case IrOperandKind.Immediate:
                sb.Append("  MOV AX, ");
                sb.AppendLine(operand.Value.ToString());
                break;

            case IrOperandKind.Register:
                var offset = GetRegisterBpOffset(operand.Register, frame, function);
                sb.Append("  MOV AX, [BP");
                sb.Append(offset >= 0 ? "+" : string.Empty);
                sb.Append(offset);
                sb.AppendLine("]");
                break;

            case IrOperandKind.Label:
                sb.Append("  MOV AX, ");
                sb.AppendLine(operand.Label);
                break;
        }
    }

    private static void StoreAxToOperand(StringBuilder sb, IrOperand operand, StackFrameLayout frame, IrFunction function)
    {
        if (operand.Kind != IrOperandKind.Register)
        {
            return;
        }

        var offset = GetRegisterBpOffset(operand.Register, frame, function);
        sb.Append("  MOV [BP");
        sb.Append(offset >= 0 ? "+" : string.Empty);
        sb.Append(offset);
        sb.AppendLine("], AX");
    }

    private static int GetRegisterBpOffset(int register, StackFrameLayout frame, IrFunction function)
    {
        // Parameters live above the return address: [BP+4], [BP+6], ...
        if (register < function.ParameterCount)
        {
            return StackFrameLayout.GetParameterOffset(register);
        }

        // Locals live below BP: [BP-2], [BP-4], ...
        return -frame.GetOffset(register);
    }

    private static void CollectRegisters(HashSet<int> registers, IrInstruction instr)
    {
        AddIfRegister(registers, instr.Dest);
        AddIfRegister(registers, instr.Left);
        AddIfRegister(registers, instr.Right);
    }

    private static void AddIfRegister(HashSet<int> registers, IrOperand? operand)
    {
        if (operand?.Kind == IrOperandKind.Register)
        {
            registers.Add(operand.Register);
        }
    }
}
