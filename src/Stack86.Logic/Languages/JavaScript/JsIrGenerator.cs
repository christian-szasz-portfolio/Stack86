namespace Stack86.Logic.Languages.JavaScript;

using System.Text;
using Stack86.Common.Exceptions;
using Stack86.Logic.Languages.JavaScript.Ast;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Translates a JavaScript AST into the shared three-address-code IR.
/// All JS values are mapped to 16-bit integers or string pointers on the 8086 target.
/// Top-level statements are wrapped in a synthetic <c>main</c> function.
/// </summary>
public sealed class JsIrGenerator
{
    private readonly List<IrFunction> functions = [];
    private readonly List<IrGlobalData> globals = [];
    private readonly List<IrDiagnostic> diagnostics = [];
    private readonly Dictionary<string, JsSymbolInfo> symbols = [];
    private readonly Stack<(string ContinueLabel, string BreakLabel)> loopStack = new();
    private readonly HashSet<string> declaredFunctions = [];
    private readonly Dictionary<string, int> arrayLengths = [];
    private readonly Dictionary<string, List<string>> objectFields = [];

    // Per-function state
    private List<IrInstruction> instructions = [];
    private int registerCount;
    private int labelCounter;
    private int parameterCount;
    private int arrowCounter;

    /// <summary>
    /// Generates an <see cref="IrProgram"/> from the given JS program AST.
    /// </summary>
    /// <param name="program">The root program node.</param>
    /// <returns>The compiled IR program.</returns>
    public IrProgram Generate(JsProgramNode program)
    {
        // Pre-pass: collect all object shapes so they are known before function generation
        this.CollectObjectShapes(program.Statements);

        // First pass: collect all top-level function declarations (hoisting)
        foreach (var stmt in program.Statements)
        {
            if (stmt is JsFunctionDeclaration fn)
            {
                this.declaredFunctions.Add(fn.Name);
            }
        }

        // Generate named functions first
        foreach (var stmt in program.Statements)
        {
            if (stmt is JsFunctionDeclaration fn)
            {
                this.GenerateFunction(fn.Name, fn.Parameters, fn.Body);
            }
        }

        // Wrap remaining top-level statements in main()
        var topLevelStatements = new List<JsStatementNode>();
        foreach (var stmt in program.Statements)
        {
            if (stmt is not JsFunctionDeclaration)
            {
                topLevelStatements.Add(stmt);
            }
        }

        if (topLevelStatements.Count > 0)
        {
            var mainBody = new JsBlockStatement { Statements = topLevelStatements, Line = 1, Column = 1 };
            this.GenerateFunction("main", [], mainBody);
        }
        else if (this.functions.All(f => f.Name != "main"))
        {
            // If there's no top-level code but there is a main() function,
            // it was already generated above.
            // If there's neither, create an empty main.
            this.GenerateFunction("main", [], new JsBlockStatement { Statements = [], Line = 1, Column = 1 });
        }

        return new IrProgram
        {
            Functions = this.functions,
            Globals = this.globals,
            Diagnostics = this.diagnostics,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Static helpers
    // ────────────────────────────────────────────────────────
    private static IrOpCode MapBinaryOp(JsBinaryOperator op)
    {
        return op switch
        {
            JsBinaryOperator.Add => IrOpCode.Add,
            JsBinaryOperator.Sub => IrOpCode.Sub,
            JsBinaryOperator.Mul => IrOpCode.Mul,
            JsBinaryOperator.Div => IrOpCode.Div,
            JsBinaryOperator.Mod => IrOpCode.Mod,
            JsBinaryOperator.BitwiseAnd => IrOpCode.And,
            JsBinaryOperator.BitwiseOr => IrOpCode.Or,
            JsBinaryOperator.BitwiseXor => IrOpCode.Xor,
            JsBinaryOperator.Shl => IrOpCode.Shl,
            JsBinaryOperator.Shr or JsBinaryOperator.UnsignedShr => IrOpCode.Shr,
            JsBinaryOperator.Equal or JsBinaryOperator.StrictEqual => IrOpCode.CmpEq,
            JsBinaryOperator.NotEqual or JsBinaryOperator.StrictNotEqual => IrOpCode.CmpNe,
            JsBinaryOperator.Less => IrOpCode.CmpLt,
            JsBinaryOperator.LessEqual => IrOpCode.CmpLe,
            JsBinaryOperator.Greater => IrOpCode.CmpGt,
            JsBinaryOperator.GreaterEqual => IrOpCode.CmpGe,
            _ => throw new UnsupportedSyntaxException($"Unsupported binary operator: {op}"),
        };
    }

    private static IrOpCode MapCompoundOp(JsAssignmentOperator op)
    {
        return op switch
        {
            JsAssignmentOperator.AddAssign => IrOpCode.Add,
            JsAssignmentOperator.SubAssign => IrOpCode.Sub,
            JsAssignmentOperator.MulAssign => IrOpCode.Mul,
            JsAssignmentOperator.DivAssign => IrOpCode.Div,
            JsAssignmentOperator.ModAssign => IrOpCode.Mod,
            JsAssignmentOperator.AndAssign => IrOpCode.And,
            JsAssignmentOperator.OrAssign => IrOpCode.Or,
            JsAssignmentOperator.XorAssign => IrOpCode.Xor,
            JsAssignmentOperator.ShlAssign => IrOpCode.Shl,
            JsAssignmentOperator.ShrAssign or JsAssignmentOperator.UnsignedShrAssign => IrOpCode.Shr,
            _ => throw new UnsupportedSyntaxException($"Not a compound operator: {op}"),
        };
    }

    private static bool IsComparisonOp(JsBinaryOperator op)
    {
        return op is JsBinaryOperator.Equal or JsBinaryOperator.NotEqual
            or JsBinaryOperator.StrictEqual or JsBinaryOperator.StrictNotEqual
            or JsBinaryOperator.Less or JsBinaryOperator.LessEqual
            or JsBinaryOperator.Greater or JsBinaryOperator.GreaterEqual;
    }

    // ────────────────────────────────────────────────────────
    //  Object shape pre-collection
    // ────────────────────────────────────────────────────────
    private void CollectObjectShapes(IReadOnlyList<JsStatementNode> statements)
    {
        foreach (var stmt in statements)
        {
            switch (stmt)
            {
                case JsVariableDeclaration { Initializer: JsObjectLiteral objLit } decl:
                    this.objectFields[decl.Name] = [.. objLit.Properties.Select(p => p.Key)];
                    this.CollectNestedObjectShapes(decl.Name, objLit);
                    break;
                case JsFunctionDeclaration fn:
                    this.CollectObjectShapes(fn.Body.Statements);
                    break;
                case JsBlockStatement block:
                    this.CollectObjectShapes(block.Statements);
                    break;
                case JsIfStatement ifStmt:
                    this.CollectObjectShapes([ifStmt.Consequent]);
                    if (ifStmt.Alternate is not null)
                    {
                        this.CollectObjectShapes([ifStmt.Alternate]);
                    }

                    break;
                case JsWhileStatement whileStmt:
                    this.CollectObjectShapes([whileStmt.Body]);
                    break;
                case JsForStatement forStmt:
                    this.CollectObjectShapes([forStmt.Body]);
                    break;
            }
        }
    }

    private void CollectNestedObjectShapes(string parentName, JsObjectLiteral objLit)
    {
        foreach (var prop in objLit.Properties)
        {
            if (prop.Value is JsObjectLiteral nested)
            {
                var key = $"__{parentName}_{prop.Key}";
                this.objectFields[key] = [.. nested.Properties.Select(p => p.Key)];
                this.CollectNestedObjectShapes(key, nested);
            }
        }
    }

    private int? FindFieldOffsetByDuckType(string propertyName)
    {
        foreach (var fields in this.objectFields.Values)
        {
            var index = fields.IndexOf(propertyName);
            if (index >= 0)
            {
                return index;
            }
        }

        return null;
    }

    // ────────────────────────────────────────────────────────
    //  Function generation
    // ────────────────────────────────────────────────────────
    private void GenerateFunction(string name, IReadOnlyList<string> parameters, JsBlockStatement body)
    {
        // Preserve global symbols across function boundaries
        var outerSymbols = new Dictionary<string, JsSymbolInfo>(this.symbols);
        var outerInstructions = this.instructions;
        var outerRegisterCount = this.registerCount;
        var outerParamCount = this.parameterCount;

        this.instructions = [];
        this.registerCount = 0;
        this.parameterCount = parameters.Count;

        // Clear non-global symbols for this function scope
        var globalSymbols = this.symbols
            .Where(s => s.Value.IsGlobal)
            .ToDictionary(s => s.Key, s => s.Value);
        this.symbols.Clear();
        foreach (var (k, v) in globalSymbols)
        {
            this.symbols[k] = v;
        }

        // Allocate registers for parameters
        foreach (var param in parameters)
        {
            var reg = this.AllocateRegister();
            this.symbols[param] = new JsSymbolInfo(reg, JsInferredType.Number, IsGlobal: false, IsConst: false);
        }

        // Generate body
        this.GenerateBlock(body);

        // Implicit return
        if (this.instructions.Count == 0 || this.instructions[^1].OpCode != IrOpCode.Return)
        {
            this.Emit(new IrInstruction { OpCode = IrOpCode.Return });
        }

        var registerSizes = new Dictionary<int, int>();
        foreach (var sym in this.symbols.Values)
        {
            if (!sym.IsGlobal && sym.Type == JsInferredType.Array)
            {
                registerSizes[sym.Register] = sym.ArrayLength * 2;
            }
            else if (!sym.IsGlobal && sym.Type == JsInferredType.Object)
            {
                registerSizes[sym.Register] = sym.FieldCount * 2;
            }
        }

        this.functions.Add(new IrFunction
        {
            Name = name,
            ParameterCount = parameters.Count,
            RegisterCount = this.registerCount,
            Instructions = this.instructions,
            RegisterSizes = registerSizes,
        });

        // Restore outer scope
        this.symbols.Clear();
        foreach (var (k, v) in outerSymbols)
        {
            this.symbols[k] = v;
        }

        this.instructions = outerInstructions;
        this.registerCount = outerRegisterCount;
        this.parameterCount = outerParamCount;
    }

    // ────────────────────────────────────────────────────────
    //  Statements
    // ────────────────────────────────────────────────────────
    private void GenerateBlock(JsBlockStatement block)
    {
        foreach (var stmt in block.Statements)
        {
            this.GenerateStatement(stmt);
        }
    }

    private void GenerateStatement(JsStatementNode stmt)
    {
        switch (stmt)
        {
            case JsBlockStatement block:
                this.GenerateBlock(block);
                break;
            case JsVariableDeclaration varDecl:
                this.GenerateVarDecl(varDecl);
                break;
            case JsFunctionDeclaration fn:
                // Nested function declarations — generate as separate IrFunction
                this.declaredFunctions.Add(fn.Name);
                this.GenerateFunction(fn.Name, fn.Parameters, fn.Body);
                break;
            case JsIfStatement ifStmt:
                this.GenerateIf(ifStmt);
                break;
            case JsWhileStatement whileStmt:
                this.GenerateWhile(whileStmt);
                break;
            case JsDoWhileStatement doWhile:
                this.GenerateDoWhile(doWhile);
                break;
            case JsForStatement forStmt:
                this.GenerateFor(forStmt);
                break;
            case JsForOfStatement forOf:
                this.GenerateForOf(forOf);
                break;
            case JsSwitchStatement switchStmt:
                this.GenerateSwitch(switchStmt);
                break;
            case JsReturnStatement ret:
                this.GenerateReturn(ret);
                break;
            case JsBreakStatement:
                if (this.loopStack.Count > 0)
                {
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Jump,
                        Left = IrOperand.Lbl(this.loopStack.Peek().BreakLabel),
                    });
                }

                break;
            case JsContinueStatement:
                if (this.loopStack.Count > 0)
                {
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Jump,
                        Left = IrOperand.Lbl(this.loopStack.Peek().ContinueLabel),
                    });
                }

                break;
            case JsExpressionStatement exprStmt:
                this.GenerateExpression(exprStmt.Expression);
                break;
        }
    }

    private void GenerateVarDecl(JsVariableDeclaration decl)
    {
        var reg = this.AllocateRegister();
        var inferredType = JsInferredType.Number;

        if (decl.Initializer is not null)
        {
            var (initReg, initType) = this.GenerateExpression(decl.Initializer);
            inferredType = initType;

            if (initType == JsInferredType.Array && decl.Initializer is JsArrayLiteral arrLit)
            {
                // Array: already allocated on stack during GenerateExpression
                // The register from the array literal is the base address
                this.symbols[decl.Name] = new JsSymbolInfo(initReg, JsInferredType.Array, false, decl.Kind == JsVariableKind.Const)
                {
                    ArrayLength = arrLit.Elements.Count,
                };
                this.arrayLengths[decl.Name] = arrLit.Elements.Count;
                return;
            }

            if (initType == JsInferredType.Object && decl.Initializer is JsObjectLiteral objLit)
            {
                // Object: already allocated on stack during GenerateExpression
                this.symbols[decl.Name] = new JsSymbolInfo(initReg, JsInferredType.Object, false, decl.Kind == JsVariableKind.Const)
                {
                    FieldCount = objLit.Properties.Count,
                };
                this.objectFields[decl.Name] = [.. objLit.Properties.Select(p => p.Key)];
                return;
            }

            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.Copy,
                Dest = IrOperand.Reg(reg),
                Left = IrOperand.Reg(initReg),
                SourceLine = decl.Line,
            });
        }

        this.symbols[decl.Name] = new JsSymbolInfo(reg, inferredType, IsGlobal: false, IsConst: decl.Kind == JsVariableKind.Const);
    }

    private void GenerateIf(JsIfStatement ifStmt)
    {
        var (condReg, _) = this.GenerateExpression(ifStmt.Condition);
        var elseLabel = this.NewLabel("if_else");
        var endLabel = this.NewLabel("if_end");

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfZero,
            Left = IrOperand.Reg(condReg),
            Right = IrOperand.Lbl(ifStmt.Alternate != null ? elseLabel : endLabel),
        });

        this.GenerateStatement(ifStmt.Consequent);

        if (ifStmt.Alternate != null)
        {
            this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(endLabel) });
            this.EmitLabel(elseLabel);
            this.GenerateStatement(ifStmt.Alternate);
        }

        this.EmitLabel(endLabel);
    }

    private void GenerateWhile(JsWhileStatement whileStmt)
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

    private void GenerateDoWhile(JsDoWhileStatement doWhile)
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

    private void GenerateFor(JsForStatement forStmt)
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

        if (forStmt.Update != null)
        {
            this.GenerateExpression(forStmt.Update);
        }

        this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(condLabel) });
        this.EmitLabel(endLabel);
        this.loopStack.Pop();
    }

    private void GenerateForOf(JsForOfStatement forOf)
    {
        // Desugar for...of into an indexed for loop:
        //   let _i = 0; while (_i < arr.length) { let x = arr[_i]; body; _i++; }
        var (iterableReg, _) = this.GenerateExpression(forOf.Iterable);

        // Determine the array length
        var arrayLength = 0;
        if (forOf.Iterable is JsIdentifierExpression idExpr && this.arrayLengths.TryGetValue(idExpr.Name, out var len))
        {
            arrayLength = len;
        }

        // Create index variable
        var indexReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(indexReg),
            Left = IrOperand.Imm(0),
            SourceLine = forOf.Line,
        });

        var condLabel = this.NewLabel("forof_cond");
        var incrLabel = this.NewLabel("forof_incr");
        var endLabel = this.NewLabel("forof_end");

        this.loopStack.Push((incrLabel, endLabel));
        this.EmitLabel(condLabel);

        // Condition: _i < length
        var lenReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(lenReg),
            Left = IrOperand.Imm(arrayLength),
        });

        var cmpReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.CmpLt,
            Dest = IrOperand.Reg(cmpReg),
            Left = IrOperand.Reg(indexReg),
            Right = IrOperand.Reg(lenReg),
        });
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfZero,
            Left = IrOperand.Reg(cmpReg),
            Right = IrOperand.Lbl(endLabel),
        });

        // Load element: let x = arr[_i]
        var elemAddrReg = this.GenerateArrayIndexAddress(iterableReg, indexReg, forOf.Line);
        var elemReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadMem,
            Dest = IrOperand.Reg(elemReg),
            Left = IrOperand.Reg(elemAddrReg),
            SourceLine = forOf.Line,
        });
        this.symbols[forOf.Variable] = new JsSymbolInfo(elemReg, JsInferredType.Number, false, forOf.Kind == JsVariableKind.Const);

        // Body
        this.GenerateStatement(forOf.Body);

        // Increment
        this.EmitLabel(incrLabel);
        var oneReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(oneReg),
            Left = IrOperand.Imm(1),
        });
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Add,
            Dest = IrOperand.Reg(indexReg),
            Left = IrOperand.Reg(indexReg),
            Right = IrOperand.Reg(oneReg),
        });

        this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(condLabel) });
        this.EmitLabel(endLabel);
        this.loopStack.Pop();
    }

    private void GenerateSwitch(JsSwitchStatement switchStmt)
    {
        var (exprReg, _) = this.GenerateExpression(switchStmt.Discriminant);
        var endLabel = this.NewLabel("switch_end");

        this.loopStack.Push((endLabel, endLabel));

        var caseLabels = new List<string>(switchStmt.Cases.Count);
        string? defaultLabel = null;

        // Generate comparison chain
        foreach (var clause in switchStmt.Cases)
        {
            var label = this.NewLabel("case");
            caseLabels.Add(label);

            if (clause.Test == null)
            {
                defaultLabel = label;
            }
            else
            {
                var (caseValReg, _) = this.GenerateExpression(clause.Test);
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

    private void GenerateReturn(JsReturnStatement ret)
    {
        IrOperand? value = null;

        if (ret.Value != null)
        {
            var (retReg, _) = this.GenerateExpression(ret.Value);
            value = IrOperand.Reg(retReg);
        }

        this.Emit(new IrInstruction { OpCode = IrOpCode.Return, Left = value, SourceLine = ret.Line });
    }

    // ────────────────────────────────────────────────────────
    //  Expressions → returns (register, inferred type)
    // ────────────────────────────────────────────────────────
    private (int Register, JsInferredType Type) GenerateExpression(JsExpressionNode expr)
    {
        return expr switch
        {
            JsIntegerLiteral lit => this.GenerateIntLiteral(lit),
            JsStringLiteral lit => this.GenerateStringLiteral(lit),
            JsBooleanLiteral lit => this.GenerateBoolLiteral(lit),
            JsNullLiteral nul => this.GenerateNullLiteral(nul),
            JsUndefinedLiteral undef => this.GenerateUndefinedLiteral(undef),
            JsIdentifierExpression id => this.GenerateIdentifier(id),
            JsBinaryExpression bin => this.GenerateBinary(bin),
            JsUnaryExpression un => this.GenerateUnary(un),
            JsUpdateExpression upd => this.GenerateUpdate(upd),
            JsAssignmentExpression assign => this.GenerateAssignment(assign),
            JsCallExpression call => this.GenerateCall(call),
            JsMemberExpression member => this.GenerateMemberAccess(member),
            JsArrayAccessExpression arr => this.GenerateArrayAccess(arr),
            JsTernaryExpression tern => this.GenerateTernary(tern),
            JsArrayLiteral arrLit => this.GenerateArrayLiteral(arrLit),
            JsObjectLiteral objLit => this.GenerateObjectLiteral(objLit),
            JsTemplateLiteral tmpl => this.GenerateTemplateLiteral(tmpl),
            JsArrowFunctionExpression arrow => this.GenerateArrowFunction(arrow),
            JsCommaExpression comma => this.GenerateComma(comma),
            _ => throw new UnsupportedSyntaxException($"Unsupported JS expression: {expr.GetType().Name}"),
        };
    }

    private (int Register, JsInferredType Type) GenerateIntLiteral(JsIntegerLiteral lit)
    {
        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Imm(lit.Value),
            SourceLine = lit.Line,
        });
        return (reg, JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateStringLiteral(JsStringLiteral lit)
    {
        var label = $"_str_{this.globals.Count}";
        var bytes = Encoding.ASCII.GetBytes(lit.Value + '$');
        this.globals.Add(new IrGlobalData { Label = label, Bytes = bytes });

        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Lbl(label),
            SourceLine = lit.Line,
        });
        return (reg, JsInferredType.String);
    }

    private (int Register, JsInferredType Type) GenerateBoolLiteral(JsBooleanLiteral lit)
    {
        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Imm(lit.Value ? 1 : 0),
            SourceLine = lit.Line,
        });
        return (reg, JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateNullLiteral(JsNullLiteral nul)
    {
        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Imm(0),
            SourceLine = nul.Line,
        });
        return (reg, JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateUndefinedLiteral(JsUndefinedLiteral undef)
    {
        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Imm(0),
            SourceLine = undef.Line,
        });
        return (reg, JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateIdentifier(JsIdentifierExpression id)
    {
        if (this.symbols.TryGetValue(id.Name, out var symbol))
        {
            return (symbol.Register, symbol.Type);
        }

        this.diagnostics.Add(new IrDiagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = $"Undefined variable '{id.Name}'.",
            Line = id.Line,
            Column = id.Column,
        });
        return (this.AllocateRegister(), JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateBinary(JsBinaryExpression bin)
    {
        // Short-circuit logical &&
        if (bin.Operator == JsBinaryOperator.LogicalAnd)
        {
            return this.GenerateLogicalAnd(bin);
        }

        // Short-circuit logical ||
        if (bin.Operator == JsBinaryOperator.LogicalOr)
        {
            return this.GenerateLogicalOr(bin);
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

        var resultType = IsComparisonOp(bin.Operator) ? JsInferredType.Number : leftType;
        return (destReg, resultType);
    }

    private (int Register, JsInferredType Type) GenerateLogicalAnd(JsBinaryExpression bin)
    {
        var (leftReg, _) = this.GenerateExpression(bin.Left);
        var resultReg = this.AllocateRegister();
        var endLabel = this.NewLabel("and_end");

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Reg(leftReg),
        });
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfZero,
            Left = IrOperand.Reg(resultReg),
            Right = IrOperand.Lbl(endLabel),
        });

        var (rightReg, _) = this.GenerateExpression(bin.Right);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Reg(rightReg),
        });

        this.EmitLabel(endLabel);
        return (resultReg, JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateLogicalOr(JsBinaryExpression bin)
    {
        var (leftReg, _) = this.GenerateExpression(bin.Left);
        var resultReg = this.AllocateRegister();
        var endLabel = this.NewLabel("or_end");

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Reg(leftReg),
        });
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfNotZero,
            Left = IrOperand.Reg(resultReg),
            Right = IrOperand.Lbl(endLabel),
        });

        var (rightReg, _) = this.GenerateExpression(bin.Right);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Reg(rightReg),
        });

        this.EmitLabel(endLabel);
        return (resultReg, JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateUnary(JsUnaryExpression un)
    {
        if (un.Operator == JsUnaryOperator.Typeof)
        {
            return this.GenerateTypeof(un);
        }

        var (operandReg, operandType) = this.GenerateExpression(un.Operand);
        var destReg = this.AllocateRegister();

        switch (un.Operator)
        {
            case JsUnaryOperator.Negate:
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Neg,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(operandReg),
                    SourceLine = un.Line,
                });
                return (destReg, JsInferredType.Number);

            case JsUnaryOperator.Plus:
                // Unary + is a no-op for numbers (just copy)
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Copy,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(operandReg),
                    SourceLine = un.Line,
                });
                return (destReg, JsInferredType.Number);

            case JsUnaryOperator.LogicalNot:
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
                return (destReg, JsInferredType.Number);

            case JsUnaryOperator.BitwiseNot:
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Not,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(operandReg),
                    SourceLine = un.Line,
                });
                return (destReg, JsInferredType.Number);

            default:
                return (destReg, operandType);
        }
    }

    private (int Register, JsInferredType Type) GenerateTypeof(JsUnaryExpression un)
    {
        // Compile-time typeof: infer type from the operand
        var typeStr = "number";
        if (un.Operand is JsStringLiteral or JsTemplateLiteral)
        {
            typeStr = "string";
        }
        else if (un.Operand is JsBooleanLiteral)
        {
            typeStr = "boolean";
        }
        else if (un.Operand is JsUndefinedLiteral)
        {
            typeStr = "undefined";
        }
        else if (un.Operand is JsNullLiteral)
        {
            typeStr = "object"; // typeof null === "object" in JS
        }
        else if (un.Operand is JsArrowFunctionExpression)
        {
            typeStr = "function";
        }
        else if (un.Operand is JsIdentifierExpression id && this.symbols.TryGetValue(id.Name, out var sym))
        {
            typeStr = sym.Type switch
            {
                JsInferredType.String => "string",
                JsInferredType.Array or JsInferredType.Object => "object",
                _ => "number",
            };
        }

        return this.GenerateStringLiteral(new JsStringLiteral { Value = typeStr, Line = un.Line, Column = un.Column });
    }

    private (int Register, JsInferredType Type) GenerateUpdate(JsUpdateExpression upd)
    {
        if (upd.Operand is not JsIdentifierExpression id || !this.symbols.TryGetValue(id.Name, out var sym))
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = "Increment/decrement requires a variable.",
                Line = upd.Line,
                Column = upd.Column,
            });
            return (this.AllocateRegister(), JsInferredType.Number);
        }

        if (sym.IsConst)
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = $"Assignment to constant variable '{id.Name}'.",
                Line = upd.Line,
                Column = upd.Column,
            });
        }

        if (upd.IsPrefix)
        {
            // ++x / --x: modify then return
            var oneReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadImm,
                Dest = IrOperand.Reg(oneReg),
                Left = IrOperand.Imm(1),
            });

            var op = upd.IsIncrement ? IrOpCode.Add : IrOpCode.Sub;
            this.Emit(new IrInstruction
            {
                OpCode = op,
                Dest = IrOperand.Reg(sym.Register),
                Left = IrOperand.Reg(sym.Register),
                Right = IrOperand.Reg(oneReg),
                SourceLine = upd.Line,
            });

            return (sym.Register, JsInferredType.Number);
        }
        else
        {
            // x++ / x--: save old value, modify, return old
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

            var op = upd.IsIncrement ? IrOpCode.Add : IrOpCode.Sub;
            this.Emit(new IrInstruction
            {
                OpCode = op,
                Dest = IrOperand.Reg(sym.Register),
                Left = IrOperand.Reg(sym.Register),
                Right = IrOperand.Reg(oneReg),
                SourceLine = upd.Line,
            });

            return (oldReg, JsInferredType.Number);
        }
    }

    private (int Register, JsInferredType Type) GenerateAssignment(JsAssignmentExpression assign)
    {
        if (assign.Operator == JsAssignmentOperator.Assign)
        {
            return this.GenerateSimpleAssignment(assign);
        }

        return this.GenerateCompoundAssignment(assign);
    }

    private (int Register, JsInferredType Type) GenerateSimpleAssignment(JsAssignmentExpression assign)
    {
        var (valueReg, valueType) = this.GenerateExpression(assign.Value);

        if (assign.Target is JsIdentifierExpression id)
        {
            if (this.symbols.TryGetValue(id.Name, out var sym))
            {
                if (sym.IsConst)
                {
                    this.diagnostics.Add(new IrDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Assignment to constant variable '{id.Name}'.",
                        Line = assign.Line,
                        Column = assign.Column,
                    });
                }

                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Copy,
                    Dest = IrOperand.Reg(sym.Register),
                    Left = IrOperand.Reg(valueReg),
                    SourceLine = assign.Line,
                });
                return (sym.Register, sym.Type);
            }

            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = $"Undefined variable '{id.Name}'.",
                Line = id.Line,
                Column = id.Column,
            });
        }
        else if (assign.Target is JsArrayAccessExpression arrAccess)
        {
            this.GenerateArrayStore(arrAccess, valueReg, assign.Line);
        }
        else if (assign.Target is JsMemberExpression memberExpr)
        {
            this.GenerateObjectPropertyStore(memberExpr, valueReg, assign.Line);
        }

        return (valueReg, valueType);
    }

    private (int Register, JsInferredType Type) GenerateCompoundAssignment(JsAssignmentExpression assign)
    {
        if (assign.Target is JsIdentifierExpression id && this.symbols.TryGetValue(id.Name, out var sym))
        {
            if (sym.IsConst)
            {
                this.diagnostics.Add(new IrDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Assignment to constant variable '{id.Name}'.",
                    Line = assign.Line,
                    Column = assign.Column,
                });
            }

            var (valueReg, _) = this.GenerateExpression(assign.Value);
            var opCode = MapCompoundOp(assign.Operator);

            this.Emit(new IrInstruction
            {
                OpCode = opCode,
                Dest = IrOperand.Reg(sym.Register),
                Left = IrOperand.Reg(sym.Register),
                Right = IrOperand.Reg(valueReg),
                SourceLine = assign.Line,
            });

            return (sym.Register, sym.Type);
        }

        this.diagnostics.Add(new IrDiagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = "Compound assignment requires a variable.",
            Line = assign.Line,
            Column = assign.Column,
        });
        return (this.AllocateRegister(), JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateCall(JsCallExpression call)
    {
        // Check for console.log / console.warn / console.error
        if (call.Callee is JsMemberExpression member
            && member.Object is JsIdentifierExpression consoleId
            && consoleId.Name == "console"
            && member.Property is "log" or "warn" or "error" or "info")
        {
            return this.GenerateConsoleLog(call);
        }

        // Get the function name from the callee
        string? funcName = null;
        if (call.Callee is JsIdentifierExpression funcId)
        {
            funcName = funcId.Name;
        }
        else if (call.Callee is JsMemberExpression mem &&
                 mem.Object is JsIdentifierExpression objId)
        {
            funcName = $"{objId.Name}_{mem.Property}";
        }

        if (funcName is null)
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = "Unsupported call expression.",
                Line = call.Line,
                Column = call.Column,
            });
            return (this.AllocateRegister(), JsInferredType.Number);
        }

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
            Left = IrOperand.Lbl(funcName),
            Right = IrOperand.Imm(call.Arguments.Count),
            SourceLine = call.Line,
        });

        return (destReg, JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateConsoleLog(JsCallExpression call)
    {
        // console.log(a, b, c) → print each argument separated by spaces, ending with newline
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            if (i > 0)
            {
                // Print space separator
                var spaceReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadImm,
                    Dest = IrOperand.Reg(spaceReg),
                    Left = IrOperand.Imm(' '),
                    SourceLine = call.Line,
                });
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.PrintChar,
                    Left = IrOperand.Reg(spaceReg),
                    SourceLine = call.Line,
                });
            }

            var arg = call.Arguments[i];

            // Handle template literals inline for better output
            if (arg is JsTemplateLiteral tmpl)
            {
                this.EmitTemplatePrint(tmpl, call.Line);
                continue;
            }

            var (argReg, argType) = this.GenerateExpression(arg);

            if (argType == JsInferredType.String)
            {
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.PrintStr,
                    Left = IrOperand.Reg(argReg),
                    SourceLine = call.Line,
                });
            }
            else
            {
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.PrintInt,
                    Left = IrOperand.Reg(argReg),
                    SourceLine = call.Line,
                });
            }
        }

        // Trailing newline
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

        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Imm(0),
            SourceLine = call.Line,
        });
        return (destReg, JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateMemberAccess(JsMemberExpression member)
    {
        // array.length → compile-time constant
        if (member.Property == "length"
            && member.Object is JsIdentifierExpression arrId
            && this.arrayLengths.TryGetValue(arrId.Name, out var arrLen))
        {
            var reg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadImm,
                Dest = IrOperand.Reg(reg),
                Left = IrOperand.Imm(arrLen),
                SourceLine = member.Line,
            });
            return (reg, JsInferredType.Number);
        }

        // obj.property → load from object register via offset
        if (member.Object is JsIdentifierExpression objId
            && this.symbols.TryGetValue(objId.Name, out var objSym))
        {
            // Direct lookup or duck-type fallback
            int fieldIndex;
            if (this.objectFields.TryGetValue(objId.Name, out var fields))
            {
                fieldIndex = fields.IndexOf(member.Property);
            }
            else
            {
                fieldIndex = this.FindFieldOffsetByDuckType(member.Property) ?? -1;
            }

            if (fieldIndex >= 0)
            {
                var addrReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadAddress,
                    Dest = IrOperand.Reg(addrReg),
                    Left = IrOperand.Reg(objSym.Register),
                    SourceLine = member.Line,
                });

                if (fieldIndex > 0)
                {
                    var offsetReg = this.AllocateRegister();
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.LoadImm,
                        Dest = IrOperand.Reg(offsetReg),
                        Left = IrOperand.Imm(fieldIndex * 2),
                    });
                    var newAddrReg = this.AllocateRegister();
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Add,
                        Dest = IrOperand.Reg(newAddrReg),
                        Left = IrOperand.Reg(addrReg),
                        Right = IrOperand.Reg(offsetReg),
                    });
                    addrReg = newAddrReg;
                }

                var destReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadMem,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(addrReg),
                    SourceLine = member.Line,
                });
                return (destReg, JsInferredType.Number);
            }
        }

        // Chained member access (e.g. r.origin.x): generate the inner expression
        // which yields an address (nested objects store their address), then resolve
        // the outer property via duck typing.
        if (member.Object is JsExpressionNode)
        {
            var (innerReg, _) = this.GenerateExpression(member.Object);
            var fieldIndex = this.FindFieldOffsetByDuckType(member.Property) ?? -1;

            if (fieldIndex >= 0)
            {
                var addrReg = innerReg;
                if (fieldIndex > 0)
                {
                    var offsetReg = this.AllocateRegister();
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.LoadImm,
                        Dest = IrOperand.Reg(offsetReg),
                        Left = IrOperand.Imm(fieldIndex * 2),
                    });
                    var newAddrReg = this.AllocateRegister();
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Add,
                        Dest = IrOperand.Reg(newAddrReg),
                        Left = IrOperand.Reg(addrReg),
                        Right = IrOperand.Reg(offsetReg),
                    });
                    addrReg = newAddrReg;
                }

                var destReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadMem,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(addrReg),
                    SourceLine = member.Line,
                });
                return (destReg, JsInferredType.Number);
            }
        }

        this.diagnostics.Add(new IrDiagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = $"Cannot access property '{member.Property}'.",
            Line = member.Line,
            Column = member.Column,
        });
        return (this.AllocateRegister(), JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateArrayAccess(JsArrayAccessExpression arr)
    {
        var (baseReg, _) = this.GenerateExpression(arr.Array);
        var (indexReg, _) = this.GenerateExpression(arr.Index);

        var addrReg = this.GenerateArrayIndexAddress(baseReg, indexReg, arr.Line);

        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadMem,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Reg(addrReg),
            SourceLine = arr.Line,
        });

        return (destReg, JsInferredType.Number);
    }

    private int GenerateArrayIndexAddress(int baseReg, int indexReg, int line)
    {
        // address = base + index * 2 (16-bit elements)
        var sizeReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(sizeReg),
            Left = IrOperand.Imm(2),
        });

        var offsetReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Mul,
            Dest = IrOperand.Reg(offsetReg),
            Left = IrOperand.Reg(indexReg),
            Right = IrOperand.Reg(sizeReg),
        });

        var baseAddrReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadAddress,
            Dest = IrOperand.Reg(baseAddrReg),
            Left = IrOperand.Reg(baseReg),
            SourceLine = line,
        });

        var addrReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Add,
            Dest = IrOperand.Reg(addrReg),
            Left = IrOperand.Reg(baseAddrReg),
            Right = IrOperand.Reg(offsetReg),
        });

        return addrReg;
    }

    private void GenerateArrayStore(JsArrayAccessExpression arrAccess, int valueReg, int line)
    {
        var (baseReg, _) = this.GenerateExpression(arrAccess.Array);
        var (indexReg, _) = this.GenerateExpression(arrAccess.Index);

        var addrReg = this.GenerateArrayIndexAddress(baseReg, indexReg, line);

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.StoreMem,
            Dest = IrOperand.Reg(addrReg),
            Left = IrOperand.Reg(valueReg),
            SourceLine = line,
        });
    }

    private void GenerateObjectPropertyStore(JsMemberExpression member, int valueReg, int line)
    {
        if (member.Object is JsIdentifierExpression objId
            && this.symbols.TryGetValue(objId.Name, out var objSym))
        {
            // Direct lookup or duck-type fallback
            int fieldIndex;
            if (this.objectFields.TryGetValue(objId.Name, out var fields))
            {
                fieldIndex = fields.IndexOf(member.Property);
            }
            else
            {
                fieldIndex = this.FindFieldOffsetByDuckType(member.Property) ?? -1;
            }

            if (fieldIndex >= 0)
            {
                var addrReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadAddress,
                    Dest = IrOperand.Reg(addrReg),
                    Left = IrOperand.Reg(objSym.Register),
                    SourceLine = line,
                });

                if (fieldIndex > 0)
                {
                    var offsetReg = this.AllocateRegister();
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.LoadImm,
                        Dest = IrOperand.Reg(offsetReg),
                        Left = IrOperand.Imm(fieldIndex * 2),
                    });
                    var newAddrReg = this.AllocateRegister();
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Add,
                        Dest = IrOperand.Reg(newAddrReg),
                        Left = IrOperand.Reg(addrReg),
                        Right = IrOperand.Reg(offsetReg),
                    });
                    addrReg = newAddrReg;
                }

                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.StoreMem,
                    Dest = IrOperand.Reg(addrReg),
                    Left = IrOperand.Reg(valueReg),
                    SourceLine = line,
                });
                return;
            }
        }

        this.diagnostics.Add(new IrDiagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Message = $"Cannot set property '{member.Property}'.",
            Line = line,
            Column = 0,
        });
    }

    private (int Register, JsInferredType Type) GenerateTernary(JsTernaryExpression tern)
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

        var (thenReg, thenType) = this.GenerateExpression(tern.Consequent);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Reg(thenReg),
        });
        this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(endLabel) });

        this.EmitLabel(elseLabel);
        var (elseReg, _) = this.GenerateExpression(tern.Alternate);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Reg(elseReg),
        });

        this.EmitLabel(endLabel);
        return (resultReg, thenType);
    }

    private (int Register, JsInferredType Type) GenerateArrayLiteral(JsArrayLiteral arrLit)
    {
        // Allocate a register for the array (will be a stack-allocated block)
        var arrReg = this.AllocateRegister();

        // Store each element at offset i * 2
        for (var i = 0; i < arrLit.Elements.Count; i++)
        {
            var (elemReg, _) = this.GenerateExpression(arrLit.Elements[i]);

            // Compute address: base + i * 2
            var addrReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadAddress,
                Dest = IrOperand.Reg(addrReg),
                Left = IrOperand.Reg(arrReg),
                SourceLine = arrLit.Line,
            });

            if (i > 0)
            {
                var offsetReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadImm,
                    Dest = IrOperand.Reg(offsetReg),
                    Left = IrOperand.Imm(i * 2),
                });
                var newAddrReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Add,
                    Dest = IrOperand.Reg(newAddrReg),
                    Left = IrOperand.Reg(addrReg),
                    Right = IrOperand.Reg(offsetReg),
                });
                addrReg = newAddrReg;
            }

            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.StoreMem,
                Dest = IrOperand.Reg(addrReg),
                Left = IrOperand.Reg(elemReg),
                SourceLine = arrLit.Line,
            });
        }

        return (arrReg, JsInferredType.Array);
    }

    private (int Register, JsInferredType Type) GenerateObjectLiteral(JsObjectLiteral objLit)
    {
        // Objects are struct-like: allocate N * 2 bytes on stack
        var objReg = this.AllocateRegister();

        for (var i = 0; i < objLit.Properties.Count; i++)
        {
            var (valReg, valType) = this.GenerateExpression(objLit.Properties[i].Value);

            // For nested objects, store the address so chained access (a.b.c) works
            if (valType == JsInferredType.Object)
            {
                var innerAddrReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadAddress,
                    Dest = IrOperand.Reg(innerAddrReg),
                    Left = IrOperand.Reg(valReg),
                    SourceLine = objLit.Line,
                });
                valReg = innerAddrReg;
            }

            var addrReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadAddress,
                Dest = IrOperand.Reg(addrReg),
                Left = IrOperand.Reg(objReg),
                SourceLine = objLit.Line,
            });

            if (i > 0)
            {
                var offsetReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadImm,
                    Dest = IrOperand.Reg(offsetReg),
                    Left = IrOperand.Imm(i * 2),
                });
                var newAddrReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Add,
                    Dest = IrOperand.Reg(newAddrReg),
                    Left = IrOperand.Reg(addrReg),
                    Right = IrOperand.Reg(offsetReg),
                });
                addrReg = newAddrReg;
            }

            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.StoreMem,
                Dest = IrOperand.Reg(addrReg),
                Left = IrOperand.Reg(valReg),
                SourceLine = objLit.Line,
            });
        }

        return (objReg, JsInferredType.Object);
    }

    private (int Register, JsInferredType Type) GenerateTemplateLiteral(JsTemplateLiteral tmpl)
    {
        // Template literals outside of console.log: just concatenate by printing
        // and return a dummy value. In practice, template literals are most useful
        // inside console.log where we expand them inline.
        // For standalone use, we emit the sequence of prints.
        this.EmitTemplatePrint(tmpl, tmpl.Line);
        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Imm(0),
            SourceLine = tmpl.Line,
        });
        return (reg, JsInferredType.String);
    }

    private void EmitTemplatePrint(JsTemplateLiteral tmpl, int line)
    {
        foreach (var part in tmpl.Parts)
        {
            if (part is JsTemplateString str)
            {
                if (str.Value.Length > 0)
                {
                    this.EmitPrintStr(str.Value, line);
                }
            }
            else if (part is JsTemplateExpression exprPart)
            {
                var (exprReg, exprType) = this.GenerateExpression(exprPart.Expression);
                if (exprType == JsInferredType.String)
                {
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.PrintStr,
                        Left = IrOperand.Reg(exprReg),
                        SourceLine = line,
                    });
                }
                else
                {
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.PrintInt,
                        Left = IrOperand.Reg(exprReg),
                        SourceLine = line,
                    });
                }
            }
        }
    }

    private (int Register, JsInferredType Type) GenerateArrowFunction(JsArrowFunctionExpression arrow)
    {
        // Generate as a named function with a synthetic name
        var name = $"_arrow_{this.arrowCounter++}";
        this.declaredFunctions.Add(name);

        if (arrow.Body is not null)
        {
            this.GenerateFunction(name, arrow.Parameters, arrow.Body);
        }
        else if (arrow.Expression is not null)
        {
            // Concise body: wrap in a return statement
            var body = new JsBlockStatement
            {
                Statements =
                [
                    new JsReturnStatement { Value = arrow.Expression, Line = arrow.Line, Column = arrow.Column },
                ],
                Line = arrow.Line,
                Column = arrow.Column,
            };
            this.GenerateFunction(name, arrow.Parameters, body);
        }

        // Return the function "pointer" as a label
        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Lbl(name),
            SourceLine = arrow.Line,
        });
        return (reg, JsInferredType.Number);
    }

    private (int Register, JsInferredType Type) GenerateComma(JsCommaExpression comma)
    {
        (int Register, JsInferredType Type) result = (0, JsInferredType.Number);
        foreach (var expr in comma.Expressions)
        {
            result = this.GenerateExpression(expr);
        }

        return result;
    }

    // ────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────
    private void EmitPrintStr(string text, int sourceLine)
    {
        var label = $"_str_{this.globals.Count}";
        var bytes = Encoding.ASCII.GetBytes(text + '$');
        this.globals.Add(new IrGlobalData { Label = label, Bytes = bytes });

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.PrintStr,
            Left = IrOperand.Lbl(label),
            SourceLine = sourceLine,
        });
    }

    private int AllocateRegister() => this.registerCount++;

    private string NewLabel(string prefix) => $"_{prefix}_{this.labelCounter++}";

    private void Emit(IrInstruction instruction) => this.instructions.Add(instruction);

    private void EmitLabel(string name) =>
        this.Emit(new IrInstruction { OpCode = IrOpCode.Label, Left = IrOperand.Lbl(name) });
}
