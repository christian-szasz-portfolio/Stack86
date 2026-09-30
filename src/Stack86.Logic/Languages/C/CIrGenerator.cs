namespace Stack86.Logic.Languages.C;

using Stack86.Common.Exceptions;

using Stack86.Logic.Pipeline.Ast;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Translates a C AST into the three-address-code IR.
/// Performs name resolution and basic type checking along the way.
/// </summary>
public sealed class CIrGenerator(IReadOnlySet<string> systemHeaders)
{
    private readonly List<IrFunction> functions = [];
    private readonly List<IrGlobalData> globals = [];
    private readonly List<IrDiagnostic> diagnostics = [];
    private readonly CTypeResolver typeResolver = new();
    private readonly Dictionary<string, SymbolInfo> symbols = [];
    private readonly Stack<(string ContinueLabel, string BreakLabel)> loopStack = new();
    private readonly HashSet<string> declaredFunctions = [];
    private readonly Dictionary<string, FunctionDeclaration> functionDeclarations = [];

    // Per-function state
    private List<IrInstruction> instructions = [];
    private int registerCount;
    private int labelCounter;
    private int parameterCount;

    /// <summary>
    /// Generates an <see cref="IrProgram"/> from the given AST.
    /// </summary>
    /// <param name="program">The root program node.</param>
    /// <returns>The compiled IR program.</returns>
    public IrProgram Generate(ProgramNode program)
    {
        // First pass: collect all declared function names (including forward declarations)
        foreach (var decl in program.Declarations)
        {
            if (decl is FunctionDeclaration fn)
            {
                this.declaredFunctions.Add(fn.Name);
                this.functionDeclarations[fn.Name] = fn;
            }
        }

        foreach (var decl in program.Declarations)
        {
            switch (decl)
            {
                case FunctionDeclaration fn:
                    this.GenerateFunction(fn);
                    break;
                case StructDeclaration structDecl:
                    this.RegisterStruct(structDecl);
                    break;
                case EnumDeclaration enumDecl:
                    this.RegisterEnum(enumDecl);
                    break;
                case UnionDeclaration unionDecl:
                    this.RegisterUnion(unionDecl);
                    break;
                case TypedefDeclaration typedefDecl:
                    this.RegisterTypedef(typedefDecl);
                    break;
                case VariableDeclaration globalVar:
                    this.GenerateGlobalVariable(globalVar);
                    break;
            }
        }

        return new IrProgram
        {
            Functions = this.functions,
            Globals = this.globals,
            Diagnostics = this.diagnostics,
            StructDefinitions = this.BuildStructDefinitions(),
        };
    }

    // ────────────────────────────────────────────────────────
    //  Static helpers
    // ────────────────────────────────────────────────────────
    private static IrOpCode GetStoreOp(CTypeInfo pointerType)
    {
        if (pointerType is CPointerTypeInfo ptr && ptr.Inner.SizeInBytes == 1)
        {
            return IrOpCode.StoreMem8;
        }

        return IrOpCode.StoreMem;
    }

    private static IrOpCode MapBinaryOp(BinaryOperator op)
    {
        return op switch
        {
            BinaryOperator.Add => IrOpCode.Add,
            BinaryOperator.Sub => IrOpCode.Sub,
            BinaryOperator.Mul => IrOpCode.Mul,
            BinaryOperator.Div => IrOpCode.Div,
            BinaryOperator.Mod => IrOpCode.Mod,
            BinaryOperator.BitwiseAnd => IrOpCode.And,
            BinaryOperator.BitwiseOr => IrOpCode.Or,
            BinaryOperator.BitwiseXor => IrOpCode.Xor,
            BinaryOperator.Shl => IrOpCode.Shl,
            BinaryOperator.Shr => IrOpCode.Shr,
            BinaryOperator.Equal => IrOpCode.CmpEq,
            BinaryOperator.NotEqual => IrOpCode.CmpNe,
            BinaryOperator.Less => IrOpCode.CmpLt,
            BinaryOperator.LessEqual => IrOpCode.CmpLe,
            BinaryOperator.Greater => IrOpCode.CmpGt,
            BinaryOperator.GreaterEqual => IrOpCode.CmpGe,
            BinaryOperator.And => IrOpCode.And,
            BinaryOperator.Or => IrOpCode.Or,
            _ => throw new UnsupportedSyntaxException($"Unsupported binary operator: {op}"),
        };
    }

    private static bool IsComparisonOp(BinaryOperator op)
    {
        return op is BinaryOperator.Equal or BinaryOperator.NotEqual
            or BinaryOperator.Less or BinaryOperator.LessEqual
            or BinaryOperator.Greater or BinaryOperator.GreaterEqual;
    }

    // ────────────────────────────────────────────────────────
    //  Top-level declarations
    // ────────────────────────────────────────────────────────
    private List<IrStructDefinition> BuildStructDefinitions()
    {
        var defs = new List<IrStructDefinition>();
        foreach (var (name, structType) in this.typeResolver.Structs)
        {
            var fields = structType.Fields.Select(f =>
                new IrStructField(f.Name, f.Type.SizeInBytes)).ToList();
            defs.Add(new IrStructDefinition(name, fields));
        }

        return defs;
    }

    private void RegisterStruct(StructDeclaration structDecl)
    {
        this.typeResolver.RegisterStruct(structDecl.Name, structDecl.Fields);
    }

    private void RegisterEnum(EnumDeclaration enumDecl)
    {
        var members = new List<CEnumMember>(enumDecl.Members.Count);
        var nextValue = 0;
        foreach (var m in enumDecl.Members)
        {
            if (m.Value is IntegerLiteral lit)
            {
                nextValue = lit.Value;
            }

            members.Add(new CEnumMember(m.Name, nextValue));
            nextValue++;
        }

        this.typeResolver.RegisterEnum(enumDecl.Name, members);
    }

    private void RegisterUnion(UnionDeclaration unionDecl)
    {
        this.typeResolver.RegisterUnion(unionDecl.Name, unionDecl.Fields);
    }

    private void RegisterTypedef(TypedefDeclaration typedefDecl)
    {
        var resolved = this.typeResolver.Resolve(typedefDecl.OriginalType);
        this.typeResolver.RegisterTypedef(typedefDecl.AliasName, resolved);
    }

    private void GenerateGlobalVariable(VariableDeclaration globalVar)
    {
        var type = this.typeResolver.Resolve(globalVar.Type);
        var label = $"_global_{globalVar.Name}";
        var bytes = new byte[type.SizeInBytes];

        if (globalVar.Initializer is IntegerLiteral initLit)
        {
            var value = initLit.Value;
            bytes[0] = (byte)(value & 0xFF);
            if (type.SizeInBytes >= 2)
            {
                bytes[1] = (byte)((value >> 8) & 0xFF);
            }
        }

        this.globals.Add(new IrGlobalData { Label = label, Bytes = bytes });
        var reg = this.AllocateRegister();
        this.symbols[globalVar.Name] = new SymbolInfo(reg, type, IsGlobal: true, IsParameter: false);
    }

    // ────────────────────────────────────────────────────────
    //  Functions
    // ────────────────────────────────────────────────────────
    private void GenerateFunction(FunctionDeclaration fn)
    {
        // Skip forward declarations — they have no body to generate.
        if (fn.Body is null)
        {
            return;
        }

        // Preserve global symbols across function boundaries
        var globalSymbols = this.symbols
            .Where(s => s.Value.IsGlobal)
            .ToList();

        this.instructions = [];
        this.symbols.Clear();
        this.loopStack.Clear();
        this.registerCount = 0;
        this.parameterCount = fn.Parameters.Count;

        // Restore global symbols
        foreach (var (name, info) in globalSymbols)
        {
            this.symbols[name] = info;
        }

        // Allocate registers for parameters
        foreach (var param in fn.Parameters)
        {
            var paramType = this.typeResolver.Resolve(param.Type);
            var reg = this.AllocateRegister();
            this.symbols[param.Name] = new SymbolInfo(reg, paramType, IsGlobal: false, IsParameter: true);
        }

        // Generate body
        this.GenerateBlock(fn.Body!);

        // Implicit return for void functions
        if (this.instructions.Count == 0 || this.instructions[^1].OpCode != IrOpCode.Return)
        {
            this.Emit(new IrInstruction { OpCode = IrOpCode.Return });
        }

        var registerSizes = new Dictionary<int, int>();
        foreach (var sym in this.symbols.Values)
        {
            if (!sym.IsParameter && !sym.IsGlobal && sym.Type.SizeInBytes > 2)
            {
                registerSizes[sym.Register] = sym.Type.SizeInBytes;
            }
        }

        this.functions.Add(new IrFunction
        {
            Name = fn.Name,
            ParameterCount = fn.Parameters.Count,
            RegisterCount = this.registerCount,
            Instructions = this.instructions,
            RegisterSizes = registerSizes,
        });
    }

    // ────────────────────────────────────────────────────────
    //  Statements
    // ────────────────────────────────────────────────────────
    private void GenerateBlock(BlockStatement block)
    {
        foreach (var stmt in block.Statements)
        {
            this.GenerateStatement(stmt);
        }
    }

    private void GenerateStatement(StatementNode stmt)
    {
        switch (stmt)
        {
            case BlockStatement block:
                this.GenerateBlock(block);
                break;
            case ReturnStatement ret:
                this.GenerateReturn(ret);
                break;
            case IfStatement ifStmt:
                this.GenerateIf(ifStmt);
                break;
            case WhileStatement whileStmt:
                this.GenerateWhile(whileStmt);
                break;
            case ForStatement forStmt:
                this.GenerateFor(forStmt);
                break;
            case DoWhileStatement doWhile:
                this.GenerateDoWhile(doWhile);
                break;
            case SwitchStatement switchStmt:
                this.GenerateSwitch(switchStmt);
                break;
            case ExpressionStatement exprStmt:
                this.GenerateExpression(exprStmt.Expression);
                break;
            case VariableDeclarationStatement varDecl:
                this.GenerateVarDecl(varDecl.Declaration);
                break;
            case BreakStatement:
                if (this.loopStack.Count > 0)
                {
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Jump,
                        Left = IrOperand.Lbl(this.loopStack.Peek().BreakLabel),
                    });
                }

                break;
            case ContinueStatement:
                if (this.loopStack.Count > 0)
                {
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Jump,
                        Left = IrOperand.Lbl(this.loopStack.Peek().ContinueLabel),
                    });
                }

                break;
        }
    }

    private void GenerateReturn(ReturnStatement ret)
    {
        IrOperand? value = null;

        if (ret.Expression != null)
        {
            var (retReg, _) = this.GenerateExpression(ret.Expression);
            value = IrOperand.Reg(retReg);
        }

        this.Emit(new IrInstruction { OpCode = IrOpCode.Return, Left = value, SourceLine = ret.Line });
    }

    private void GenerateIf(IfStatement ifStmt)
    {
        var (condReg, _) = this.GenerateExpression(ifStmt.Condition);
        var elseLabel = this.NewLabel("if_else");
        var endLabel = this.NewLabel("if_end");

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfZero,
            Left = IrOperand.Reg(condReg),
            Right = IrOperand.Lbl(ifStmt.Else != null ? elseLabel : endLabel),
        });

        this.GenerateStatement(ifStmt.Then);

        if (ifStmt.Else != null)
        {
            this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(endLabel) });
            this.EmitLabel(elseLabel);
            this.GenerateStatement(ifStmt.Else);
        }

        this.EmitLabel(endLabel);
    }

    private void GenerateWhile(WhileStatement whileStmt)
    {
        var condLabel = this.NewLabel("while_cond");
        var endLabel = this.NewLabel("while_end");

        this.loopStack.Push((condLabel, endLabel));
        this.EmitLabel(condLabel);

        var (condReg, _) = this.GenerateExpression(whileStmt.Condition);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfZero,
            Left = IrOperand.Reg(condReg),
            Right = IrOperand.Lbl(endLabel),
        });

        this.GenerateStatement(whileStmt.Body);
        this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(condLabel) });
        this.EmitLabel(endLabel);
        this.loopStack.Pop();
    }

    private void GenerateFor(ForStatement forStmt)
    {
        if (forStmt.Init != null)
        {
            this.GenerateStatement(forStmt.Init);
        }

        var condLabel = this.NewLabel("for_cond");
        var incrLabel = this.NewLabel("for_incr");
        var endLabel = this.NewLabel("for_end");

        this.loopStack.Push((incrLabel, endLabel));
        this.EmitLabel(condLabel);

        if (forStmt.Condition != null)
        {
            var (condReg, _) = this.GenerateExpression(forStmt.Condition);
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.JumpIfZero,
                Left = IrOperand.Reg(condReg),
                Right = IrOperand.Lbl(endLabel),
            });
        }

        this.GenerateStatement(forStmt.Body);
        this.EmitLabel(incrLabel);

        if (forStmt.Increment != null)
        {
            this.GenerateExpression(forStmt.Increment);
        }

        this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(condLabel) });
        this.EmitLabel(endLabel);
        this.loopStack.Pop();
    }

    private void GenerateDoWhile(DoWhileStatement doWhile)
    {
        var bodyLabel = this.NewLabel("dowhile_body");
        var condLabel = this.NewLabel("dowhile_cond");
        var endLabel = this.NewLabel("dowhile_end");

        this.loopStack.Push((condLabel, endLabel));
        this.EmitLabel(bodyLabel);

        this.GenerateStatement(doWhile.Body);

        this.EmitLabel(condLabel);
        var (condReg, _) = this.GenerateExpression(doWhile.Condition);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfNotZero,
            Left = IrOperand.Reg(condReg),
            Right = IrOperand.Lbl(bodyLabel),
        });

        this.EmitLabel(endLabel);
        this.loopStack.Pop();
    }

    private void GenerateSwitch(SwitchStatement switchStmt)
    {
        var (exprReg, _) = this.GenerateExpression(switchStmt.Expression);
        var endLabel = this.NewLabel("switch_end");

        // Push break target so break statements jump to switch end
        this.loopStack.Push((endLabel, endLabel));

        var caseLabels = new List<string>(switchStmt.Cases.Count);
        string? defaultLabel = null;

        // Generate comparison chain
        foreach (var clause in switchStmt.Cases)
        {
            var label = this.NewLabel("case");
            caseLabels.Add(label);

            if (clause.Value == null)
            {
                // default:
                defaultLabel = label;
            }
            else
            {
                var (caseValReg, _) = this.GenerateExpression(clause.Value);
                var cmpReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.CmpEq,
                    Dest = IrOperand.Reg(cmpReg),
                    Left = IrOperand.Reg(exprReg),
                    Right = IrOperand.Reg(caseValReg),
                });
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.JumpIfNotZero,
                    Left = IrOperand.Reg(cmpReg),
                    Right = IrOperand.Lbl(label),
                });
            }
        }

        // Jump to default if present, otherwise end
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Jump,
            Left = IrOperand.Lbl(defaultLabel ?? endLabel),
        });

        // Generate case bodies (fall-through)
        for (var i = 0; i < switchStmt.Cases.Count; i++)
        {
            this.EmitLabel(caseLabels[i]);
            foreach (var bodyStmt in switchStmt.Cases[i].Body)
            {
                this.GenerateStatement(bodyStmt);
            }
        }

        this.EmitLabel(endLabel);
        this.loopStack.Pop();
    }

    private void GenerateVarDecl(VariableDeclaration decl)
    {
        var type = this.typeResolver.Resolve(decl.Type);
        var reg = this.AllocateRegister();
        this.symbols[decl.Name] = new SymbolInfo(reg, type, IsGlobal: false, IsParameter: false);

        if (decl.Initializer != null)
        {
            var (initReg, _) = this.GenerateExpression(decl.Initializer);

            if (!type.IsScalar)
            {
                // Struct/union/array: block copy
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.BlockCopy,
                    Dest = IrOperand.Reg(reg),
                    Left = IrOperand.Reg(initReg),
                    Right = IrOperand.Imm(type.SizeInBytes),
                    SourceLine = decl.Line,
                });
            }
            else
            {
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Copy,
                    Dest = IrOperand.Reg(reg),
                    Left = IrOperand.Reg(initReg),
                    SourceLine = decl.Line,
                });
            }
        }
    }

    // ────────────────────────────────────────────────────────
    //  Expressions → returns the virtual register + type
    // ────────────────────────────────────────────────────────
    private (int Register, CTypeInfo Type) GenerateExpression(ExpressionNode expr)
    {
        return expr switch
        {
            IntegerLiteral lit => this.GenerateIntLiteral(lit),
            CharLiteral lit => this.GenerateCharLiteral(lit),
            StringLiteral lit => this.GenerateStringLiteral(lit),
            IdentifierExpression id => this.GenerateIdentifier(id),
            BinaryExpression bin => this.GenerateBinary(bin),
            UnaryExpression un => this.GenerateUnary(un),
            PostfixExpression post => this.GeneratePostfix(post),
            CallExpression call => this.GenerateCall(call),
            CastExpression cast => this.GenerateCast(cast),
            ArrayAccessExpression arr => this.GenerateArrayAccess(arr),
            MemberAccessExpression member => this.GenerateMemberAccess(member),
            TernaryExpression tern => this.GenerateTernary(tern),
            CompoundAssignmentExpression compound => this.GenerateCompoundAssignment(compound),
            SizeofExpression sizeofExpr => this.GenerateSizeof(sizeofExpr),
            CommaExpression comma => this.GenerateComma(comma),
            _ => throw new UnsupportedSyntaxException($"Unsupported expression node: {expr.GetType().Name}"),
        };
    }

    private (int Register, CTypeInfo Type) GenerateIntLiteral(IntegerLiteral lit)
    {
        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Imm(lit.Value),
            SourceLine = lit.Line,
        });
        return (reg, CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GenerateCharLiteral(CharLiteral lit)
    {
        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Imm(lit.Value),
            SourceLine = lit.Line,
        });
        return (reg, CPrimitiveTypeInfo.Char);
    }

    private (int Register, CTypeInfo Type) GenerateStringLiteral(StringLiteral lit)
    {
        var label = $"_str_{this.globals.Count}";
        var bytes = System.Text.Encoding.ASCII.GetBytes(lit.Value + '\0');
        this.globals.Add(new IrGlobalData { Label = label, Bytes = bytes });

        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Lbl(label),
            SourceLine = lit.Line,
        });
        return (reg, new CPointerTypeInfo(CPrimitiveTypeInfo.Char));
    }

    private (int Register, CTypeInfo Type) GenerateIdentifier(IdentifierExpression id)
    {
        // Check local/parameter symbols first
        if (this.symbols.TryGetValue(id.Name, out var symbol))
        {
            // Array-to-pointer decay: when an array identifier is used as a
            // value expression, it decays to a pointer to its first element.
            if (symbol.Type is CArrayTypeInfo arrayType)
            {
                var addrReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadAddress,
                    Dest = IrOperand.Reg(addrReg),
                    Left = IrOperand.Reg(symbol.Register),
                    SourceLine = id.Line,
                });
                return (addrReg, new CPointerTypeInfo(arrayType.Element));
            }

            return (symbol.Register, symbol.Type);
        }

        // A bare function name used as a value decays to a function pointer.
        if (this.declaredFunctions.Contains(id.Name))
        {
            var fpReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadFunctionAddress,
                Dest = IrOperand.Reg(fpReg),
                Left = IrOperand.Lbl(id.Name),
                SourceLine = id.Line,
            });
            return (fpReg, this.ResolveFunctionPointerType(id.Name));
        }

        // Check enum constants
        if (this.typeResolver.TryResolveEnumConstant(id.Name, out var enumValue))
        {
            var reg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadImm,
                Dest = IrOperand.Reg(reg),
                Left = IrOperand.Imm(enumValue),
                SourceLine = id.Line,
            });
            return (reg, CPrimitiveTypeInfo.Int);
        }

        this.diagnostics.Add(new IrDiagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = $"Undefined variable '{id.Name}'.",
            Line = id.Line,
            Column = id.Column,
        });

        return (this.AllocateRegister(), CPrimitiveTypeInfo.Int); // dummy
    }

    private CFunctionPointerTypeInfo ResolveFunctionPointerType(string functionName)
    {
        if (this.functionDeclarations.TryGetValue(functionName, out var fn))
        {
            var returnType = this.typeResolver.Resolve(fn.ReturnType);
            var parameterTypes = new List<CTypeInfo>(fn.Parameters.Count);
            foreach (var param in fn.Parameters)
            {
                parameterTypes.Add(this.typeResolver.Resolve(param.Type));
            }

            return new CFunctionPointerTypeInfo(returnType, parameterTypes);
        }

        return new CFunctionPointerTypeInfo(CPrimitiveTypeInfo.Int, []);
    }

    private (int Register, CTypeInfo Type) GenerateBinary(BinaryExpression bin)
    {
        // Assignment is special
        if (bin.Operator == BinaryOperator.Assign)
        {
            return this.GenerateAssignment(bin);
        }

        var (leftReg, leftType) = this.GenerateExpression(bin.Left);
        var (rightReg, _) = this.GenerateExpression(bin.Right);
        var destReg = this.AllocateRegister();

        var opCode = MapBinaryOp(bin.Operator);

        this.Emit(new IrInstruction
        {
            OpCode = opCode,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Reg(leftReg),
            Right = IrOperand.Reg(rightReg),
            SourceLine = bin.Line,
        });

        // Comparison operators produce int (0/1)
        var resultType = IsComparisonOp(bin.Operator) ? CPrimitiveTypeInfo.Int : leftType;
        return (destReg, resultType);
    }

    private (int Register, CTypeInfo Type) GenerateAssignment(BinaryExpression bin)
    {
        var (valueReg, valueType) = this.GenerateExpression(bin.Right);

        if (bin.Left is IdentifierExpression id)
        {
            if (this.symbols.TryGetValue(id.Name, out var symbol))
            {
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Copy,
                    Dest = IrOperand.Reg(symbol.Register),
                    Left = IrOperand.Reg(valueReg),
                    SourceLine = bin.Line,
                });
                return (symbol.Register, symbol.Type);
            }

            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = $"Undefined variable '{id.Name}'.",
                Line = id.Line,
                Column = id.Column,
            });
        }
        else if (bin.Left is UnaryExpression { Operator: UnaryOperator.Dereference } deref)
        {
            var (addrReg, addrType) = this.GenerateExpression(deref.Operand);
            var storeOp = GetStoreOp(addrType);
            this.Emit(new IrInstruction
            {
                OpCode = storeOp,
                Dest = IrOperand.Reg(addrReg),
                Left = IrOperand.Reg(valueReg),
                SourceLine = bin.Line,
            });
        }
        else if (bin.Left is ArrayAccessExpression arrLhs)
        {
            var (addrReg, elemType) = this.GenerateArrayAddress(arrLhs);
            var storeOp = elemType.SizeInBytes == 1 ? IrOpCode.StoreMem8 : IrOpCode.StoreMem;
            this.Emit(new IrInstruction
            {
                OpCode = storeOp,
                Dest = IrOperand.Reg(addrReg),
                Left = IrOperand.Reg(valueReg),
                SourceLine = bin.Line,
            });
        }
        else if (bin.Left is MemberAccessExpression memberLhs)
        {
            var (addrReg, fieldType) = this.GenerateMemberAddress(memberLhs);
            var storeOp = fieldType.SizeInBytes == 1 ? IrOpCode.StoreMem8 : IrOpCode.StoreMem;
            this.Emit(new IrInstruction
            {
                OpCode = storeOp,
                Dest = IrOperand.Reg(addrReg),
                Left = IrOperand.Reg(valueReg),
                SourceLine = bin.Line,
            });
        }

        return (valueReg, valueType);
    }

    private (int Register, CTypeInfo Type) GenerateUnary(UnaryExpression un)
    {
        switch (un.Operator)
        {
            case UnaryOperator.PreIncrement:
            case UnaryOperator.PreDecrement:
                return this.GeneratePreIncDec(un);
            case UnaryOperator.AddressOf:
                return this.GenerateAddressOf(un);
            default:
                break;
        }

        var (operandReg, operandType) = this.GenerateExpression(un.Operand);
        var destReg = this.AllocateRegister();

        switch (un.Operator)
        {
            case UnaryOperator.Negate:
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Neg,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(operandReg),
                    SourceLine = un.Line,
                });
                return (destReg, operandType);

            case UnaryOperator.BitwiseNot:
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Not,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(operandReg),
                    SourceLine = un.Line,
                });
                return (destReg, operandType);

            case UnaryOperator.LogicalNot:
                var zeroReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadImm,
                    Dest = IrOperand.Reg(zeroReg),
                    Left = IrOperand.Imm(0),
                });
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.CmpEq,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(operandReg),
                    Right = IrOperand.Reg(zeroReg),
                    SourceLine = un.Line,
                });
                return (destReg, CPrimitiveTypeInfo.Int);

            case UnaryOperator.Dereference:
                var innerType = operandType is CPointerTypeInfo ptr ? ptr.Inner : CPrimitiveTypeInfo.Int;
                var loadOp = innerType.SizeInBytes == 1 ? IrOpCode.LoadMem8 : IrOpCode.LoadMem;
                this.Emit(new IrInstruction
                {
                    OpCode = loadOp,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(operandReg),
                    SourceLine = un.Line,
                });
                return (destReg, innerType);

            default:
                return (destReg, operandType);
        }
    }

    private (int Register, CTypeInfo Type) GenerateAddressOf(UnaryExpression un)
    {
        if (un.Operand is IdentifierExpression id && this.symbols.TryGetValue(id.Name, out var sym))
        {
            var destReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadAddress,
                Dest = IrOperand.Reg(destReg),
                Left = IrOperand.Reg(sym.Register),
                SourceLine = un.Line,
            });
            return (destReg, new CPointerTypeInfo(sym.Type));
        }

        // Fallback: evaluate operand and return its register as a pseudo-address
        var (opReg, opType) = this.GenerateExpression(un.Operand);
        var fallbackReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadAddress,
            Dest = IrOperand.Reg(fallbackReg),
            Left = IrOperand.Reg(opReg),
            SourceLine = un.Line,
        });
        return (fallbackReg, new CPointerTypeInfo(opType));
    }

    private (int Register, CTypeInfo Type) GeneratePreIncDec(UnaryExpression un)
    {
        if (un.Operand is not IdentifierExpression id || !this.symbols.TryGetValue(id.Name, out var sym))
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = "Pre-increment/decrement requires an lvalue.",
                Line = un.Line,
                Column = un.Column,
            });
            return (this.AllocateRegister(), CPrimitiveTypeInfo.Int);
        }

        var oneReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(oneReg),
            Left = IrOperand.Imm(1),
        });

        var op = un.Operator == UnaryOperator.PreIncrement ? IrOpCode.Add : IrOpCode.Sub;
        this.Emit(new IrInstruction
        {
            OpCode = op,
            Dest = IrOperand.Reg(sym.Register),
            Left = IrOperand.Reg(sym.Register),
            Right = IrOperand.Reg(oneReg),
            SourceLine = un.Line,
        });

        return (sym.Register, sym.Type);
    }

    private (int Register, CTypeInfo Type) GeneratePostfix(PostfixExpression post)
    {
        if (post.Operand is not IdentifierExpression id || !this.symbols.TryGetValue(id.Name, out var sym))
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = "Post-increment/decrement requires an lvalue.",
                Line = post.Line,
                Column = post.Column,
            });
            return (this.AllocateRegister(), CPrimitiveTypeInfo.Int);
        }

        // Save old value
        var oldReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(oldReg),
            Left = IrOperand.Reg(sym.Register),
        });

        var oneReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(oneReg),
            Left = IrOperand.Imm(1),
        });

        var op = post.Operator == PostfixOperator.Increment ? IrOpCode.Add : IrOpCode.Sub;
        this.Emit(new IrInstruction
        {
            OpCode = op,
            Dest = IrOperand.Reg(sym.Register),
            Left = IrOperand.Reg(sym.Register),
            Right = IrOperand.Reg(oneReg),
            SourceLine = post.Line,
        });

        // Return old value
        return (oldReg, sym.Type);
    }

    private (int Register, CTypeInfo Type) GenerateCall(CallExpression call)
    {
        // A local variable holding a function pointer is called indirectly.
        if (this.symbols.TryGetValue(call.FunctionName, out var fpSymbol) &&
            fpSymbol.Type is CFunctionPointerTypeInfo fpType)
        {
            return this.GenerateIndirectCall(call, fpSymbol, fpType);
        }

        // User-defined functions always take precedence over library functions
        if (this.declaredFunctions.Contains(call.FunctionName))
        {
            return this.GenerateUserCall(call);
        }

        // Check for known library functions
        if (CLibraryRegistry.TryGetFunction(call.FunctionName, out var libFunc))
        {
            if (!CLibraryRegistry.IsHeaderIncluded(libFunc, systemHeaders))
            {
                this.diagnostics.Add(new IrDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Function '{call.FunctionName}' requires #include <{libFunc.Header}>.",
                    Line = call.Line,
                    Column = call.Column,
                });
                return (this.AllocateRegister(), CPrimitiveTypeInfo.Int);
            }

            return libFunc.Kind switch
            {
                LibraryFunctionKind.Intrinsic => this.GenerateIntrinsic(call, libFunc),
                LibraryFunctionKind.Syscall => this.GenerateSyscall(call, libFunc),
                _ => this.GenerateUserCall(call),
            };
        }

        // Unknown function — compile-time error
        this.diagnostics.Add(new IrDiagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = $"Undefined function '{call.FunctionName}'.",
            Line = call.Line,
            Column = call.Column,
        });
        return (this.AllocateRegister(), CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GenerateUserCall(CallExpression call)
    {
        // Push arguments right-to-left (C calling convention)
        for (var i = call.Arguments.Count - 1; i >= 0; i--)
        {
            var (argReg, _) = this.GenerateExpression(call.Arguments[i]);
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.Push,
                Left = IrOperand.Reg(argReg),
                SourceLine = call.Line,
            });
        }

        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Call,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Lbl(call.FunctionName),
            Right = IrOperand.Imm(call.Arguments.Count),
            SourceLine = call.Line,
        });

        return (destReg, CPrimitiveTypeInfo.Int); // return type not tracked yet
    }

    private (int Register, CTypeInfo Type) GenerateIndirectCall(
        CallExpression call,
        SymbolInfo fpSymbol,
        CFunctionPointerTypeInfo fpType)
    {
        // Push arguments right-to-left (C calling convention)
        for (var i = call.Arguments.Count - 1; i >= 0; i--)
        {
            var (argReg, _) = this.GenerateExpression(call.Arguments[i]);
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.Push,
                Left = IrOperand.Reg(argReg),
                SourceLine = call.Line,
            });
        }

        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.CallIndirect,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Reg(fpSymbol.Register),
            Right = IrOperand.Imm(call.Arguments.Count),
            SourceLine = call.Line,
        });

        return (destReg, fpType.ReturnType);
    }

    private (int Register, CTypeInfo Type) GenerateSyscall(CallExpression call, CLibraryFunction libFunc)
    {
        // Push arguments right-to-left (C calling convention)
        for (var i = call.Arguments.Count - 1; i >= 0; i--)
        {
            var (argReg, _) = this.GenerateExpression(call.Arguments[i]);
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.Push,
                Left = IrOperand.Reg(argReg),
                SourceLine = call.Line,
            });
        }

        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Syscall,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Imm(libFunc.ServiceNumber),
            Right = IrOperand.Imm(call.Arguments.Count),
            SourceLine = call.Line,
        });

        return (destReg, CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GenerateIntrinsic(CallExpression call, CLibraryFunction libFunc)
    {
        return libFunc.Name switch
        {
            "printf" => this.GeneratePrintf(call),
            "scanf" => this.GenerateScanf(call),
            "putchar" => this.GeneratePutchar(call),
            "puts" => this.GeneratePuts(call),
            "getchar" => this.GenerateGetchar(call),
            "abs" => this.GenerateAbs(call),
            "exit" => this.GenerateExit(call),
            _ => this.GenerateUserCall(call),
        };
    }

    private (int Register, CTypeInfo Type) GeneratePrintf(CallExpression call)
    {
        if (call.Arguments.Count == 0 || call.Arguments[0] is not StringLiteral formatLit)
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = "printf requires a string literal as the first argument.",
                Line = call.Line,
                Column = call.Column,
            });
            var errReg = this.AllocateRegister();
            return (errReg, CPrimitiveTypeInfo.Int);
        }

        var format = formatLit.Value;
        var argIndex = 1; // next variadic argument
        var buffer = new System.Text.StringBuilder();

        for (var i = 0; i < format.Length; i++)
        {
            if (format[i] == '%' && i + 1 < format.Length)
            {
                // Flush accumulated literal text
                if (buffer.Length > 0)
                {
                    this.EmitPrintStr(buffer.ToString(), call.Line);
                    buffer.Clear();
                }

                var spec = format[i + 1];
                i++; // skip specifier

                switch (spec)
                {
                    case 'd':
                        if (argIndex < call.Arguments.Count)
                        {
                            var (argReg, _) = this.GenerateExpression(call.Arguments[argIndex]);
                            this.Emit(new IrInstruction
                            {
                                OpCode = IrOpCode.PrintInt,
                                Left = IrOperand.Reg(argReg),
                                SourceLine = call.Line,
                            });
                            argIndex++;
                        }

                        break;

                    case 'c':
                        if (argIndex < call.Arguments.Count)
                        {
                            var (argReg, _) = this.GenerateExpression(call.Arguments[argIndex]);
                            this.Emit(new IrInstruction
                            {
                                OpCode = IrOpCode.PrintChar,
                                Left = IrOperand.Reg(argReg),
                                SourceLine = call.Line,
                            });
                            argIndex++;
                        }

                        break;

                    case 's':
                        if (argIndex < call.Arguments.Count)
                        {
                            var (argReg, _) = this.GenerateExpression(call.Arguments[argIndex]);

                            // If the argument is a pointer/array, emit PrintStr directly.
                            // If it's a string literal, GenerateExpression already loaded its address.
                            this.Emit(new IrInstruction
                            {
                                OpCode = IrOpCode.PrintStr,
                                Left = IrOperand.Reg(argReg),
                                SourceLine = call.Line,
                            });
                            argIndex++;
                        }

                        break;

                    case '%':
                        buffer.Append('%');
                        break;

                    default:
                        buffer.Append('%');
                        buffer.Append(spec);
                        break;
                }
            }
            else
            {
                buffer.Append(format[i]);
            }
        }

        // Flush remaining literal text
        if (buffer.Length > 0)
        {
            this.EmitPrintStr(buffer.ToString(), call.Line);
        }

        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Imm(0),
            SourceLine = call.Line,
        });
        return (destReg, CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GenerateScanf(CallExpression call)
    {
        if (call.Arguments.Count < 2 || call.Arguments[0] is not StringLiteral formatLit)
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = "scanf requires a format string literal and at least one pointer argument.",
                Line = call.Line,
                Column = call.Column,
            });
            var errReg = this.AllocateRegister();
            return (errReg, CPrimitiveTypeInfo.Int);
        }

        var format = formatLit.Value;
        var argIndex = 1;

        for (var i = 0; i < format.Length; i++)
        {
            if (format[i] != '%' || i + 1 >= format.Length)
            {
                continue;
            }

            i++;
            switch (format[i])
            {
                case 'd':
                {
                    if (argIndex >= call.Arguments.Count)
                    {
                        break;
                    }

                    // Emit ReadInt syscall (service 0x29) — returns integer in AX
                    var intReg = this.AllocateRegister();
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Syscall,
                        Dest = IrOperand.Reg(intReg),
                        Left = IrOperand.Imm(0x29),
                        Right = IrOperand.Imm(0),
                        SourceLine = call.Line,
                    });

                    // Evaluate pointer argument (e.g. &x)
                    var (ptrReg, _) = this.GenerateExpression(call.Arguments[argIndex]);
                    argIndex++;

                    // Store the read value at the pointer address (Dest=addr, Left=value)
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.StoreMem,
                        Dest = IrOperand.Reg(ptrReg),
                        Left = IrOperand.Reg(intReg),
                        SourceLine = call.Line,
                    });
                    break;
                }

                case 'c':
                {
                    if (argIndex >= call.Arguments.Count)
                    {
                        break;
                    }

                    // Emit Getchar syscall (service 0x0D) — returns char in AX
                    var charReg = this.AllocateRegister();
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Syscall,
                        Dest = IrOperand.Reg(charReg),
                        Left = IrOperand.Imm(0x0D),
                        Right = IrOperand.Imm(0),
                        SourceLine = call.Line,
                    });

                    // Store byte at pointer address
                    var (charPtrReg, _) = this.GenerateExpression(call.Arguments[argIndex]);
                    argIndex++;

                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.StoreMem8,
                        Dest = IrOperand.Reg(charPtrReg),
                        Left = IrOperand.Reg(charReg),
                        SourceLine = call.Line,
                    });
                    break;
                }

                case 's':
                {
                    if (argIndex >= call.Arguments.Count)
                    {
                        break;
                    }

                    // Evaluate buffer pointer argument
                    var (bufReg, _) = this.GenerateExpression(call.Arguments[argIndex]);
                    argIndex++;

                    // Emit ReadStr syscall (service 0x2A) — reads string into buffer address on stack
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Push,
                        Left = IrOperand.Reg(bufReg),
                        SourceLine = call.Line,
                    });

                    var strReg = this.AllocateRegister();
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Syscall,
                        Dest = IrOperand.Reg(strReg),
                        Left = IrOperand.Imm(0x2A),
                        Right = IrOperand.Imm(1),
                        SourceLine = call.Line,
                    });
                    break;
                }

                default:
                    break;
            }
        }

        // scanf returns number of items read — approximate with arg count
        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Imm(argIndex - 1),
            SourceLine = call.Line,
        });
        return (destReg, CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GeneratePutchar(CallExpression call)
    {
        if (call.Arguments.Count != 1)
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = "putchar expects exactly 1 argument.",
                Line = call.Line,
                Column = call.Column,
            });
            return (this.AllocateRegister(), CPrimitiveTypeInfo.Int);
        }

        var (argReg, _) = this.GenerateExpression(call.Arguments[0]);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.PrintChar,
            Left = IrOperand.Reg(argReg),
            SourceLine = call.Line,
        });

        // putchar returns the character written
        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Reg(argReg),
            SourceLine = call.Line,
        });
        return (destReg, CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GeneratePuts(CallExpression call)
    {
        if (call.Arguments.Count != 1)
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = "puts expects exactly 1 argument.",
                Line = call.Line,
                Column = call.Column,
            });
            return (this.AllocateRegister(), CPrimitiveTypeInfo.Int);
        }

        if (call.Arguments[0] is StringLiteral strLit)
        {
            this.EmitPrintStr(strLit.Value, call.Line);
        }
        else
        {
            // For non-literal strings, evaluate the expression and use PrintStr with the pointer
            var (argReg, _) = this.GenerateExpression(call.Arguments[0]);
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.PrintStr,
                Left = IrOperand.Reg(argReg),
                SourceLine = call.Line,
            });
        }

        // puts appends a newline
        var nlReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(nlReg),
            Left = IrOperand.Imm('\n'),
            SourceLine = call.Line,
        });
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.PrintChar,
            Left = IrOperand.Reg(nlReg),
            SourceLine = call.Line,
        });

        // puts returns a non-negative value on success
        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Imm(0),
            SourceLine = call.Line,
        });
        return (destReg, CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GenerateGetchar(CallExpression call)
    {
        // getchar() reads a character via INT 21h AH=01.
        // Emit: LoadImm dest, 0x0100 (AH=1, AL=0) then use PrintChar to trigger input.
        // Actually, we emit a Syscall with a dedicated service for consistency,
        // but since getchar is simple enough, we use the existing INT 21h AH=01 pattern.
        // The cleanest approach: emit as a zero-arg syscall with a dedicated service.
        // However, for simplicity, we can just emit it as LoadImm AH=01 + INT 21h directly.
        // Let's use the Syscall mechanism for uniformity.
        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Syscall,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Imm(0x0D), // Service 0x0D = getchar
            Right = IrOperand.Imm(0),
            SourceLine = call.Line,
        });
        return (destReg, CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GenerateAbs(CallExpression call)
    {
        if (call.Arguments.Count != 1)
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = "abs expects exactly 1 argument.",
                Line = call.Line,
                Column = call.Column,
            });
            return (this.AllocateRegister(), CPrimitiveTypeInfo.Int);
        }

        var (argReg, _) = this.GenerateExpression(call.Arguments[0]);
        var skipLabel = this.NewLabel("_abs_skip");
        var destReg = this.AllocateRegister();

        // Copy value to dest
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Reg(argReg),
            SourceLine = call.Line,
        });

        // Check if non-negative
        var cmpReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.CmpGe,
            Dest = IrOperand.Reg(cmpReg),
            Left = IrOperand.Reg(destReg),
            Right = IrOperand.Imm(0),
            SourceLine = call.Line,
        });
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfNotZero,
            Left = IrOperand.Reg(cmpReg),
            Right = IrOperand.Lbl(skipLabel),
            SourceLine = call.Line,
        });

        // Negate if negative
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Neg,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Reg(destReg),
            SourceLine = call.Line,
        });

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Label,
            Left = IrOperand.Lbl(skipLabel),
            SourceLine = call.Line,
        });

        return (destReg, CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GenerateExit(CallExpression call)
    {
        if (call.Arguments.Count != 1)
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = "exit expects exactly 1 argument.",
                Line = call.Line,
                Column = call.Column,
            });
            return (this.AllocateRegister(), CPrimitiveTypeInfo.Void);
        }

        // Evaluate exit code (stored in AL), then emit Syscall for exit (INT 86h AH=0E)
        var (argReg, _) = this.GenerateExpression(call.Arguments[0]);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Push,
            Left = IrOperand.Reg(argReg),
            SourceLine = call.Line,
        });

        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Syscall,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Imm(0x0E), // Service 0x0E = exit
            Right = IrOperand.Imm(1),
            SourceLine = call.Line,
        });

        return (destReg, CPrimitiveTypeInfo.Void);
    }

    private void EmitPrintStr(string text, int sourceLine)
    {
        var label = $"_str_{this.globals.Count}";
        var bytes = System.Text.Encoding.ASCII.GetBytes(text + '$');
        this.globals.Add(new IrGlobalData { Label = label, Bytes = bytes });

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.PrintStr,
            Left = IrOperand.Lbl(label),
            SourceLine = sourceLine,
        });
    }

    private (int Register, CTypeInfo Type) GenerateCast(CastExpression cast)
    {
        var (srcReg, _) = this.GenerateExpression(cast.Operand);
        var targetType = this.typeResolver.Resolve(cast.TargetType);

        // For scalar types of same size, just reinterpret
        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Reg(srcReg),
            SourceLine = cast.Line,
        });
        return (destReg, targetType);
    }

    private (int Register, CTypeInfo Type) GenerateArrayAccess(ArrayAccessExpression arr)
    {
        var (addrReg, elemType) = this.GenerateArrayAddress(arr);

        if (elemType.IsScalar)
        {
            var destReg = this.AllocateRegister();
            var loadOp = elemType.SizeInBytes == 1 ? IrOpCode.LoadMem8 : IrOpCode.LoadMem;

            this.Emit(new IrInstruction
            {
                OpCode = loadOp,
                Dest = IrOperand.Reg(destReg),
                Left = IrOperand.Reg(addrReg),
                SourceLine = arr.Line,
            });

            return (destReg, elemType);
        }

        // Non-scalar (struct/array): return the computed address, not a value.
        return (addrReg, elemType);
    }

    private (int Register, CTypeInfo Type) GenerateArrayAddress(ArrayAccessExpression arr)
    {
        var (baseReg, baseType) = this.GenerateExpression(arr.Array);
        var (indexReg, _) = this.GenerateExpression(arr.Index);

        CTypeInfo elemType;
        if (baseType is CArrayTypeInfo arrayType)
        {
            elemType = arrayType.Element;
        }
        else if (baseType is CPointerTypeInfo ptrType)
        {
            elemType = ptrType.Inner;
        }
        else
        {
            elemType = CPrimitiveTypeInfo.Int;
        }

        // offset = index * sizeof(element)
        var sizeReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(sizeReg),
            Left = IrOperand.Imm(elemType.SizeInBytes),
        });

        var offsetReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Mul,
            Dest = IrOperand.Reg(offsetReg),
            Left = IrOperand.Reg(indexReg),
            Right = IrOperand.Reg(sizeReg),
        });

        var addrReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Add,
            Dest = IrOperand.Reg(addrReg),
            Left = IrOperand.Reg(baseReg),
            Right = IrOperand.Reg(offsetReg),
        });

        return (addrReg, elemType);
    }

    private (int Register, CTypeInfo Type) GenerateMemberAccess(MemberAccessExpression member)
    {
        var (addrReg, fieldType) = this.GenerateMemberAddress(member);
        var destReg = this.AllocateRegister();

        if (fieldType.IsScalar)
        {
            var loadOp = fieldType.SizeInBytes == 1 ? IrOpCode.LoadMem8 : IrOpCode.LoadMem;
            this.Emit(new IrInstruction
            {
                OpCode = loadOp,
                Dest = IrOperand.Reg(destReg),
                Left = IrOperand.Reg(addrReg),
                SourceLine = member.Line,
            });
        }
        else
        {
            // Non-scalar: return address (e.g. nested struct)
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.Copy,
                Dest = IrOperand.Reg(destReg),
                Left = IrOperand.Reg(addrReg),
                SourceLine = member.Line,
            });
        }

        return (destReg, fieldType);
    }

    private (int Register, CTypeInfo Type) GenerateMemberAddress(MemberAccessExpression member)
    {
        var (objReg, objType) = this.GenerateExpression(member.Object);

        // For arrow (->), objType is a pointer to struct/union
        // For dot (.), objType is the struct/union itself
        CTypeInfo structOrUnionType;
        int baseAddrReg;

        if (member.IsArrow)
        {
            // obj is a pointer, dereference to get base address
            structOrUnionType = objType is CPointerTypeInfo ptr ? ptr.Inner : objType;
            baseAddrReg = objReg; // pointer already holds the address
        }
        else
        {
            // obj is a struct accessed via dot operator
            structOrUnionType = objType;

            if (member.Object is IdentifierExpression)
            {
                // Local struct variable: the struct data lives at the register's
                // stack slot, so we need LoadAddress to obtain the base pointer.
                baseAddrReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadAddress,
                    Dest = IrOperand.Reg(baseAddrReg),
                    Left = IrOperand.Reg(objReg),
                    SourceLine = member.Line,
                });
            }
            else
            {
                // Array access, nested member access, etc. already return the
                // address of the struct for non-scalar types — use it directly.
                baseAddrReg = objReg;
            }
        }

        if (structOrUnionType is CStructTypeInfo structInfo)
        {
            if (structInfo.TryGetField(member.MemberName, out var offset, out var fieldType))
            {
                if (offset == 0)
                {
                    return (baseAddrReg, fieldType);
                }

                var offsetReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadImm,
                    Dest = IrOperand.Reg(offsetReg),
                    Left = IrOperand.Imm(offset),
                });

                var addrReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Add,
                    Dest = IrOperand.Reg(addrReg),
                    Left = IrOperand.Reg(baseAddrReg),
                    Right = IrOperand.Reg(offsetReg),
                });
                return (addrReg, fieldType);
            }
        }
        else if (structOrUnionType is CUnionTypeInfo unionInfo)
        {
            // Union: all fields at offset 0
            if (unionInfo.TryGetField(member.MemberName, out var fieldType))
            {
                return (baseAddrReg, fieldType);
            }
        }

        this.diagnostics.Add(new IrDiagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = $"No field '{member.MemberName}' in type.",
            Line = member.Line,
            Column = member.Column,
        });
        return (baseAddrReg, CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GenerateTernary(TernaryExpression tern)
    {
        var (condReg, _) = this.GenerateExpression(tern.Condition);
        var elseLabel = this.NewLabel("tern_else");
        var endLabel = this.NewLabel("tern_end");
        var resultReg = this.AllocateRegister();

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfZero,
            Left = IrOperand.Reg(condReg),
            Right = IrOperand.Lbl(elseLabel),
        });

        var (thenReg, thenType) = this.GenerateExpression(tern.ThenExpression);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Reg(thenReg),
        });
        this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(endLabel) });

        this.EmitLabel(elseLabel);
        var (elseReg, _) = this.GenerateExpression(tern.ElseExpression);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Reg(elseReg),
        });

        this.EmitLabel(endLabel);
        return (resultReg, thenType);
    }

    private (int Register, CTypeInfo Type) GenerateCompoundAssignment(CompoundAssignmentExpression compound)
    {
        // target op= value  →  target = target op value
        if (compound.Target is IdentifierExpression id && this.symbols.TryGetValue(id.Name, out var sym))
        {
            var (valueReg, _) = this.GenerateExpression(compound.Value);
            var opCode = MapBinaryOp(compound.Operator);

            this.Emit(new IrInstruction
            {
                OpCode = opCode,
                Dest = IrOperand.Reg(sym.Register),
                Left = IrOperand.Reg(sym.Register),
                Right = IrOperand.Reg(valueReg),
                SourceLine = compound.Line,
            });

            return (sym.Register, sym.Type);
        }

        this.diagnostics.Add(new IrDiagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = "Compound assignment requires an lvalue.",
            Line = compound.Line,
            Column = compound.Column,
        });
        return (this.AllocateRegister(), CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GenerateSizeof(SizeofExpression sizeofExpr)
    {
        int size;
        if (sizeofExpr.TargetType != null)
        {
            size = this.typeResolver.Resolve(sizeofExpr.TargetType).SizeInBytes;
        }
        else if (sizeofExpr.Operand != null)
        {
            var (_, exprType) = this.GenerateExpression(sizeofExpr.Operand);
            size = exprType.SizeInBytes;
        }
        else
        {
            size = 0;
        }

        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Imm(size),
            SourceLine = sizeofExpr.Line,
        });
        return (reg, CPrimitiveTypeInfo.Int);
    }

    private (int Register, CTypeInfo Type) GenerateComma(CommaExpression comma)
    {
        this.GenerateExpression(comma.Left);
        return this.GenerateExpression(comma.Right);
    }

    // ────────────────────────────────────────────────────────
    //  Instance helpers
    // ────────────────────────────────────────────────────────
    private int AllocateRegister() => this.registerCount++;

    private string NewLabel(string prefix) => $"_{prefix}_{this.labelCounter++}";

    private void Emit(IrInstruction instruction) => this.instructions.Add(instruction);

    private void EmitLabel(string name) =>
        this.Emit(new IrInstruction { OpCode = IrOpCode.Label, Left = IrOperand.Lbl(name) });
}
