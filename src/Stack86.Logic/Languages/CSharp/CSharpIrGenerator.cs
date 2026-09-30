namespace Stack86.Logic.Languages.CSharp;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stack86.Logic.Pipeline.Ir;

using IrSeverity = Stack86.Logic.Pipeline.Ir.DiagnosticSeverity;

/// <summary>
/// Translates a Roslyn C# syntax tree into the three-address-code IR.
/// Uses the <see cref="SemanticModel"/> for type resolution and overload detection.
/// Only the subset of C# that maps to the 8086 emulator is supported.
/// </summary>
public sealed class CSharpIrGenerator
{
    private readonly List<IrFunction> functions = [];
    private readonly List<IrGlobalData> globals = [];
    private readonly List<IrDiagnostic> diagnostics = [];
    private readonly List<IrStructDefinition> structDefinitions = [];
    private readonly Dictionary<string, List<(string Name, int Offset, int Size)>> classLayouts = [];

    // Per-function state
    private List<IrInstruction> instructions = [];
    private int registerCount;
    private int labelCounter;
    private Dictionary<string, (int Register, string TypeName)> symbols = [];
    private Stack<(string ContinueLabel, string BreakLabel)> loopStack = new();
    private Dictionary<int, int> registerSizes = [];
    private SemanticModel? model;
    private string? currentClassName;

    /// <summary>
    /// Generates an <see cref="IrProgram"/> from the Roslyn compilation.
    /// </summary>
    public IrProgram Generate(CSharpCompilation compilation)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            this.model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            this.WalkCompilationUnit(root);
        }

        return new IrProgram
        {
            Functions = this.functions,
            Globals = this.globals,
            Diagnostics = this.diagnostics,
            StructDefinitions = this.structDefinitions,
        };
    }

    private static string GetTypeName(TypeSyntax? type) =>
        type?.ToString() ?? "int";

    private static int GetLine(SyntaxNode node) =>
        node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static bool IsAutoProperty(PropertyDeclarationSyntax prop)
    {
        if (prop.AccessorList is null)
        {
            return false;
        }

        foreach (var accessor in prop.AccessorList.Accessors)
        {
            if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration)
                && (accessor.Body is not null || accessor.ExpressionBody is not null))
            {
                return false;
            }
        }

        return true;
    }

    // ────────────────────────────────────────────────────────
    //  Top-level walk
    // ────────────────────────────────────────────────────────
    private void WalkCompilationUnit(SyntaxNode root)
    {
        var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Where(t => t is ClassDeclarationSyntax or StructDeclarationSyntax)
            .ToList();

        // First pass: discover class/struct field layouts
        foreach (var typeDecl in types)
        {
            this.RegisterTypeLayout(typeDecl);
        }

        // Second pass: generate methods and constructors
        foreach (var typeDecl in types)
        {
            this.currentClassName = typeDecl.Identifier.Text;

            foreach (var member in typeDecl.Members)
            {
                switch (member)
                {
                    case MethodDeclarationSyntax method when method.Body is not null:
                        this.GenerateMethod(method);
                        break;
                    case ConstructorDeclarationSyntax ctor when ctor.Body is not null:
                        this.GenerateConstructor(ctor);
                        break;
                    case PropertyDeclarationSyntax prop when !IsAutoProperty(prop):
                        this.GeneratePropertyAccessors(prop);
                        break;
                }
            }

            this.currentClassName = null;
        }
    }

    private void RegisterTypeLayout(TypeDeclarationSyntax typeDecl)
    {
        var className = typeDecl.Identifier.Text;
        var fields = new List<(string Name, int Offset, int Size)>();
        var offset = 0;

        foreach (var member in typeDecl.Members)
        {
            if (member is FieldDeclarationSyntax fieldDecl)
            {
                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    fields.Add((variable.Identifier.Text, offset, 2));
                    offset += 2;
                }
            }

            // Auto-properties (no getter body) → backing field
            if (member is PropertyDeclarationSyntax prop && IsAutoProperty(prop))
            {
                fields.Add((prop.Identifier.Text, offset, 2));
                offset += 2;
            }
        }

        if (fields.Count > 0)
        {
            this.classLayouts[className] = fields;

            var irFields = fields.Select(f => new IrStructField(f.Name, f.Size)).ToList();
            this.structDefinitions.Add(new IrStructDefinition(className, irFields));
        }
    }

    private bool TryGetField(string className, string fieldName, out (string Name, int Offset, int Size) field)
    {
        field = default;
        if (!this.classLayouts.TryGetValue(className, out var fields))
        {
            return false;
        }

        foreach (var f in fields)
        {
            if (f.Name == fieldName)
            {
                field = f;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Computes the memory address of a field relative to an object register.
    /// When <paramref name="isPointer"/> is true, the register already holds a pointer (e.g. 'this').
    /// Otherwise, LoadAddress is emitted to obtain the stack address.
    /// </summary>
    private int ComputeFieldAddress(int objReg, bool isPointer, int fieldOffset, int sourceLine)
    {
        int baseAddrReg;

        if (isPointer)
        {
            baseAddrReg = objReg;
        }
        else
        {
            baseAddrReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadAddress,
                Dest = IrOperand.Reg(baseAddrReg),
                Left = IrOperand.Reg(objReg),
                SourceLine = sourceLine,
            });
        }

        if (fieldOffset == 0)
        {
            return baseAddrReg;
        }

        var offsetReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(offsetReg),
            Left = IrOperand.Imm(fieldOffset),
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

    // ────────────────────────────────────────────────────────
    //  Methods
    // ────────────────────────────────────────────────────────
    private void GenerateMethod(MethodDeclarationSyntax method)
    {
        this.instructions = [];
        this.registerCount = 0;
        this.labelCounter = 0;
        this.symbols = [];
        this.loopStack = new();
        this.registerSizes = [];

        var isStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword);
        var isInstance = !isStatic
            && this.currentClassName is not null
            && this.classLayouts.ContainsKey(this.currentClassName);

        // For instance methods, first parameter is the implicit 'this' pointer
        if (isInstance)
        {
            var thisReg = this.AllocateRegister();
            this.symbols["this"] = (thisReg, this.currentClassName! + "*");
        }

        // Allocate registers for parameters
        foreach (var param in method.ParameterList.Parameters)
        {
            var reg = this.AllocateRegister();
            var paramTypeName = GetTypeName(param.Type);

            // Struct-typed parameters are passed by pointer
            if (this.classLayouts.ContainsKey(paramTypeName))
            {
                paramTypeName += "*";
            }

            this.symbols[param.Identifier.Text] = (reg, paramTypeName);
        }

        this.GenerateBlock(method.Body!);

        // Implicit return for void methods
        if (this.instructions.Count == 0 || this.instructions[^1].OpCode != IrOpCode.Return)
        {
            this.Emit(new IrInstruction { OpCode = IrOpCode.Return });
        }

        // Entry point: C# "Main" → IR "main" (Asm8086Generator expects lowercase)
        string funcName;
        if (method.Identifier.Text == "Main")
        {
            funcName = "main";
        }
        else if (isInstance)
        {
            funcName = $"{this.currentClassName}_{method.Identifier.Text}";
        }
        else
        {
            funcName = method.Identifier.Text;
        }

        this.functions.Add(new IrFunction
        {
            Name = funcName,
            ParameterCount = method.ParameterList.Parameters.Count + (isInstance ? 1 : 0),
            RegisterCount = this.registerCount,
            Instructions = this.instructions,
            RegisterSizes = this.registerSizes,
        });
    }

    private void GenerateConstructor(ConstructorDeclarationSyntax ctor)
    {
        this.instructions = [];
        this.registerCount = 0;
        this.labelCounter = 0;
        this.symbols = [];
        this.loopStack = new();
        this.registerSizes = [];

        // First parameter is the implicit 'this' pointer
        var thisReg = this.AllocateRegister();
        this.symbols["this"] = (thisReg, this.currentClassName + "*");

        // Allocate registers for constructor parameters
        foreach (var param in ctor.ParameterList.Parameters)
        {
            var reg = this.AllocateRegister();
            this.symbols[param.Identifier.Text] = (reg, GetTypeName(param.Type));
        }

        this.GenerateBlock(ctor.Body!);

        // Implicit return
        if (this.instructions.Count == 0 || this.instructions[^1].OpCode != IrOpCode.Return)
        {
            this.Emit(new IrInstruction { OpCode = IrOpCode.Return });
        }

        this.functions.Add(new IrFunction
        {
            Name = $"{this.currentClassName}_ctor",
            ParameterCount = ctor.ParameterList.Parameters.Count + 1,
            RegisterCount = this.registerCount,
            Instructions = this.instructions,
            RegisterSizes = this.registerSizes,
        });
    }

    private void GeneratePropertyAccessors(PropertyDeclarationSyntax prop)
    {
        if (prop.AccessorList is null)
        {
            return;
        }

        foreach (var accessor in prop.AccessorList.Accessors)
        {
            if (accessor.Body is null && accessor.ExpressionBody is null)
            {
                continue;
            }

            this.instructions = [];
            this.registerCount = 0;
            this.labelCounter = 0;
            this.symbols = [];
            this.loopStack = new();
            this.registerSizes = [];

            var thisReg = this.AllocateRegister();
            this.symbols["this"] = (thisReg, this.currentClassName + "*");

            string suffix;
            int paramCount = 1;

            if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
            {
                suffix = $"get_{prop.Identifier.Text}";
            }
            else
            {
                suffix = $"set_{prop.Identifier.Text}";
                var valueReg = this.AllocateRegister();
                this.symbols["value"] = (valueReg, GetTypeName(prop.Type));
                paramCount = 2;
            }

            if (accessor.Body is not null)
            {
                this.GenerateBlock(accessor.Body);
            }
            else if (accessor.ExpressionBody is not null)
            {
                var reg = this.GenerateExpression(accessor.ExpressionBody.Expression);
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Return,
                    Left = IrOperand.Reg(reg),
                    SourceLine = GetLine(accessor),
                });
            }

            if (this.instructions.Count == 0 || this.instructions[^1].OpCode != IrOpCode.Return)
            {
                this.Emit(new IrInstruction { OpCode = IrOpCode.Return });
            }

            this.functions.Add(new IrFunction
            {
                Name = $"{this.currentClassName}_{suffix}",
                ParameterCount = paramCount,
                RegisterCount = this.registerCount,
                Instructions = this.instructions,
                RegisterSizes = this.registerSizes,
            });
        }
    }

    // ────────────────────────────────────────────────────────
    //  Statements
    // ────────────────────────────────────────────────────────
    private void GenerateBlock(BlockSyntax block)
    {
        foreach (var stmt in block.Statements)
        {
            this.GenerateStatement(stmt);
        }
    }

    private void GenerateStatement(StatementSyntax stmt)
    {
        switch (stmt)
        {
            case LocalDeclarationStatementSyntax local:
                this.GenerateLocalDeclaration(local);
                break;
            case ExpressionStatementSyntax expr:
                this.GenerateExpression(expr.Expression);
                break;
            case ReturnStatementSyntax ret:
                this.GenerateReturn(ret);
                break;
            case IfStatementSyntax ifStmt:
                this.GenerateIf(ifStmt);
                break;
            case WhileStatementSyntax whileStmt:
                this.GenerateWhile(whileStmt);
                break;
            case ForStatementSyntax forStmt:
                this.GenerateFor(forStmt);
                break;
            case DoStatementSyntax doStmt:
                this.GenerateDoWhile(doStmt);
                break;
            case BlockSyntax block:
                this.GenerateBlock(block);
                break;
            case SwitchStatementSyntax switchStmt:
                this.GenerateSwitch(switchStmt);
                break;
            case BreakStatementSyntax:
                if (this.loopStack.Count > 0)
                {
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Jump,
                        Left = IrOperand.Lbl(this.loopStack.Peek().BreakLabel),
                    });
                }

                break;
            case ContinueStatementSyntax:
                if (this.loopStack.Count > 0)
                {
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Jump,
                        Left = IrOperand.Lbl(this.loopStack.Peek().ContinueLabel),
                    });
                }

                break;
            default:
                this.AddDiagnostic(
                    IrSeverity.Warning,
                    $"Unsupported statement: {stmt.Kind()}",
                    stmt.GetLocation());
                break;
        }
    }

    private void GenerateLocalDeclaration(LocalDeclarationStatementSyntax local)
    {
        var typeName = GetTypeName(local.Declaration.Type);

        foreach (var variable in local.Declaration.Variables)
        {
            var name = variable.Identifier.Text;
            var reg = this.AllocateRegister();
            this.symbols[name] = (reg, typeName);

            // For class types, allocate struct-sized storage
            if (this.classLayouts.TryGetValue(typeName, out var classFields))
            {
                var totalSize = classFields.Sum(f => f.Size);
                if (totalSize > 2)
                {
                    this.registerSizes[reg] = totalSize;
                }
            }

            if (variable.Initializer is not null)
            {
                // Optimised path: new ClassName(...) directly initialises the variable
                if (this.classLayouts.ContainsKey(typeName)
                    && variable.Initializer.Value is ObjectCreationExpressionSyntax creation)
                {
                    this.GenerateObjectCreationInto(creation, reg);
                }
                else
                {
                    var initReg = this.GenerateExpression(variable.Initializer.Value);
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Copy,
                        Dest = IrOperand.Reg(reg),
                        Left = IrOperand.Reg(initReg),
                        SourceLine = GetLine(variable),
                    });
                }
            }
        }
    }

    private void GenerateReturn(ReturnStatementSyntax ret)
    {
        IrOperand? value = null;
        if (ret.Expression is not null)
        {
            var reg = this.GenerateExpression(ret.Expression);
            value = IrOperand.Reg(reg);
        }

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Return,
            Left = value,
            SourceLine = GetLine(ret),
        });
    }

    private void GenerateIf(IfStatementSyntax ifStmt)
    {
        var condReg = this.GenerateExpression(ifStmt.Condition);
        var elseLabel = this.NewLabel("if_else");
        var endLabel = this.NewLabel("if_end");

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfZero,
            Left = IrOperand.Reg(condReg),
            Right = IrOperand.Lbl(ifStmt.Else is not null ? elseLabel : endLabel),
        });

        this.GenerateStatement(ifStmt.Statement);

        if (ifStmt.Else is not null)
        {
            this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(endLabel) });
            this.EmitLabel(elseLabel);
            this.GenerateStatement(ifStmt.Else.Statement);
        }

        this.EmitLabel(endLabel);
    }

    private void GenerateWhile(WhileStatementSyntax whileStmt)
    {
        var condLabel = this.NewLabel("while_cond");
        var endLabel = this.NewLabel("while_end");

        this.loopStack.Push((condLabel, endLabel));
        this.EmitLabel(condLabel);

        var condReg = this.GenerateExpression(whileStmt.Condition);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfZero,
            Left = IrOperand.Reg(condReg),
            Right = IrOperand.Lbl(endLabel),
        });

        this.GenerateStatement(whileStmt.Statement);
        this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(condLabel) });
        this.EmitLabel(endLabel);
        this.loopStack.Pop();
    }

    private void GenerateFor(ForStatementSyntax forStmt)
    {
        // Variable declaration in initializer
        if (forStmt.Declaration is not null)
        {
            var typeName = GetTypeName(forStmt.Declaration.Type);
            foreach (var variable in forStmt.Declaration.Variables)
            {
                var name = variable.Identifier.Text;
                var reg = this.AllocateRegister();
                this.symbols[name] = (reg, typeName);

                if (variable.Initializer is not null)
                {
                    var initReg = this.GenerateExpression(variable.Initializer.Value);
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.Copy,
                        Dest = IrOperand.Reg(reg),
                        Left = IrOperand.Reg(initReg),
                    });
                }
            }
        }

        // Expression initializers (e.g. for (i = 0; ...))
        foreach (var init in forStmt.Initializers)
        {
            this.GenerateExpression(init);
        }

        var condLabel = this.NewLabel("for_cond");
        var incrLabel = this.NewLabel("for_incr");
        var endLabel = this.NewLabel("for_end");

        this.loopStack.Push((incrLabel, endLabel));
        this.EmitLabel(condLabel);

        if (forStmt.Condition is not null)
        {
            var condReg = this.GenerateExpression(forStmt.Condition);
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.JumpIfZero,
                Left = IrOperand.Reg(condReg),
                Right = IrOperand.Lbl(endLabel),
            });
        }

        this.GenerateStatement(forStmt.Statement);
        this.EmitLabel(incrLabel);

        foreach (var incr in forStmt.Incrementors)
        {
            this.GenerateExpression(incr);
        }

        this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(condLabel) });
        this.EmitLabel(endLabel);
        this.loopStack.Pop();
    }

    private void GenerateDoWhile(DoStatementSyntax doStmt)
    {
        var bodyLabel = this.NewLabel("dowhile_body");
        var condLabel = this.NewLabel("dowhile_cond");
        var endLabel = this.NewLabel("dowhile_end");

        this.loopStack.Push((condLabel, endLabel));
        this.EmitLabel(bodyLabel);

        this.GenerateStatement(doStmt.Statement);

        this.EmitLabel(condLabel);
        var condReg = this.GenerateExpression(doStmt.Condition);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfNotZero,
            Left = IrOperand.Reg(condReg),
            Right = IrOperand.Lbl(bodyLabel),
        });

        this.EmitLabel(endLabel);
        this.loopStack.Pop();
    }

    private void GenerateSwitch(SwitchStatementSyntax switchStmt)
    {
        var exprReg = this.GenerateExpression(switchStmt.Expression);
        var endLabel = this.NewLabel("switch_end");

        this.loopStack.Push((endLabel, endLabel));

        var sectionLabels = new List<string>(switchStmt.Sections.Count);
        string? defaultLabel = null;

        // Generate comparison chain
        foreach (var section in switchStmt.Sections)
        {
            var label = this.NewLabel("case");
            sectionLabels.Add(label);

            foreach (var caseLabel in section.Labels)
            {
                if (caseLabel is CaseSwitchLabelSyntax caseSwitch)
                {
                    var caseReg = this.GenerateExpression(caseSwitch.Value);
                    var cmpReg = this.AllocateRegister();
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.CmpEq,
                        Dest = IrOperand.Reg(cmpReg),
                        Left = IrOperand.Reg(exprReg),
                        Right = IrOperand.Reg(caseReg),
                    });
                    this.Emit(new IrInstruction
                    {
                        OpCode = IrOpCode.JumpIfNotZero,
                        Left = IrOperand.Reg(cmpReg),
                        Right = IrOperand.Lbl(label),
                    });
                }
                else if (caseLabel is DefaultSwitchLabelSyntax)
                {
                    defaultLabel = label;
                }
            }
        }

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Jump,
            Left = IrOperand.Lbl(defaultLabel ?? endLabel),
        });

        // Generate section bodies (fall-through)
        for (var i = 0; i < switchStmt.Sections.Count; i++)
        {
            this.EmitLabel(sectionLabels[i]);
            foreach (var bodyStmt in switchStmt.Sections[i].Statements)
            {
                this.GenerateStatement(bodyStmt);
            }
        }

        this.EmitLabel(endLabel);
        this.loopStack.Pop();
    }

    // ────────────────────────────────────────────────────────
    //  Expressions → returns the virtual register
    // ────────────────────────────────────────────────────────
    private int GenerateExpression(ExpressionSyntax expr)
    {
        return expr switch
        {
            LiteralExpressionSyntax lit => this.GenerateLiteral(lit),
            IdentifierNameSyntax id => this.GenerateIdentifier(id),
            BinaryExpressionSyntax bin => this.GenerateBinary(bin),
            PrefixUnaryExpressionSyntax unary => this.GeneratePrefixUnary(unary),
            PostfixUnaryExpressionSyntax postfix => this.GeneratePostfixUnary(postfix),
            InvocationExpressionSyntax invoke => this.GenerateInvocation(invoke),
            AssignmentExpressionSyntax assign => this.GenerateAssignment(assign),
            ParenthesizedExpressionSyntax paren => this.GenerateExpression(paren.Expression),
            MemberAccessExpressionSyntax memberAccess => this.GenerateMemberAccess(memberAccess),
            CastExpressionSyntax cast => this.GenerateExpression(cast.Expression),
            ObjectCreationExpressionSyntax creation => this.GenerateObjectCreation(creation),
            ConditionalExpressionSyntax cond => this.GenerateConditional(cond),
            ThisExpressionSyntax => this.symbols.TryGetValue("this", out var thisSym)
                ? thisSym.Register
                : this.AllocateRegister(),
            _ => this.GenerateDummy(expr),
        };
    }

    private int GenerateLiteral(LiteralExpressionSyntax lit)
    {
        var reg = this.AllocateRegister();
        int value;

        if (lit.IsKind(SyntaxKind.NumericLiteralExpression) && lit.Token.Value is int intVal)
        {
            value = intVal;
        }
        else if (lit.IsKind(SyntaxKind.CharacterLiteralExpression) && lit.Token.Value is char charVal)
        {
            value = charVal;
        }
        else if (lit.IsKind(SyntaxKind.StringLiteralExpression))
        {
            // Strings as expressions (outside of print context) → store as global
            var text = lit.Token.ValueText;
            var label = $"_str_{this.globals.Count}";
            var bytes = System.Text.Encoding.ASCII.GetBytes(text + '$');
            this.globals.Add(new IrGlobalData { Label = label, Bytes = bytes });

            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadImm,
                Dest = IrOperand.Reg(reg),
                Left = IrOperand.Lbl(label),
                SourceLine = GetLine(lit),
            });
            return reg;
        }
        else if (lit.IsKind(SyntaxKind.TrueLiteralExpression))
        {
            value = 1;
        }
        else if (lit.IsKind(SyntaxKind.FalseLiteralExpression))
        {
            value = 0;
        }
        else
        {
            value = 0;
        }

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Imm(value),
            SourceLine = GetLine(lit),
        });
        return reg;
    }

    private int GenerateIdentifier(IdentifierNameSyntax id)
    {
        if (this.symbols.TryGetValue(id.Identifier.Text, out var sym))
        {
            return sym.Register;
        }

        // Try to resolve as an enum constant via the semantic model
        var constValue = this.model?.GetConstantValue(id);
        if (constValue is { HasValue: true, Value: int enumVal })
        {
            var reg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadImm,
                Dest = IrOperand.Reg(reg),
                Left = IrOperand.Imm(enumVal),
                SourceLine = GetLine(id),
            });
            return reg;
        }

        // Implicit this.field — resolve identifier as an instance field
        if (this.currentClassName is not null
            && this.symbols.TryGetValue("this", out var thisParam)
            && this.TryGetField(this.currentClassName, id.Identifier.Text, out var fieldInfo))
        {
            var addrReg = this.ComputeFieldAddress(thisParam.Register, isPointer: true, fieldInfo.Offset, GetLine(id));
            var destReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadMem,
                Dest = IrOperand.Reg(destReg),
                Left = IrOperand.Reg(addrReg),
                SourceLine = GetLine(id),
            });
            return destReg;
        }

        // Implicit static field (e.g. Count inside Counter.Increment())
        var idSymbolInfo = this.model?.GetSymbolInfo(id);
        if (idSymbolInfo?.Symbol is IFieldSymbol { IsStatic: true } staticField)
        {
            var key = $"{staticField.ContainingType.Name}.{staticField.Name}";
            if (!this.symbols.TryGetValue(key, out var existing))
            {
                var reg = this.AllocateRegister();
                existing = (reg, "int");
                this.symbols[key] = existing;
            }

            return existing.Register;
        }

        this.AddDiagnostic(
            IrSeverity.Error,
            $"Undefined variable '{id.Identifier.Text}'.",
            id.GetLocation());
        return this.AllocateRegister();
    }

    private int GenerateBinary(BinaryExpressionSyntax bin)
    {
        var opCode = bin.Kind() switch
        {
            SyntaxKind.AddExpression => IrOpCode.Add,
            SyntaxKind.SubtractExpression => IrOpCode.Sub,
            SyntaxKind.MultiplyExpression => IrOpCode.Mul,
            SyntaxKind.DivideExpression => IrOpCode.Div,
            SyntaxKind.ModuloExpression => IrOpCode.Mod,
            SyntaxKind.BitwiseAndExpression => IrOpCode.And,
            SyntaxKind.BitwiseOrExpression => IrOpCode.Or,
            SyntaxKind.ExclusiveOrExpression => IrOpCode.Xor,
            SyntaxKind.LeftShiftExpression => IrOpCode.Shl,
            SyntaxKind.RightShiftExpression => IrOpCode.Shr,
            SyntaxKind.EqualsExpression => IrOpCode.CmpEq,
            SyntaxKind.NotEqualsExpression => IrOpCode.CmpNe,
            SyntaxKind.LessThanExpression => IrOpCode.CmpLt,
            SyntaxKind.LessThanOrEqualExpression => IrOpCode.CmpLe,
            SyntaxKind.GreaterThanExpression => IrOpCode.CmpGt,
            SyntaxKind.GreaterThanOrEqualExpression => IrOpCode.CmpGe,
            SyntaxKind.LogicalAndExpression => (IrOpCode?)null,
            SyntaxKind.LogicalOrExpression => (IrOpCode?)null,
            _ => (IrOpCode?)null,
        };

        // Short-circuit logical operators
        if (bin.IsKind(SyntaxKind.LogicalAndExpression))
        {
            return this.GenerateLogicalAnd(bin);
        }

        if (bin.IsKind(SyntaxKind.LogicalOrExpression))
        {
            return this.GenerateLogicalOr(bin);
        }

        if (opCode is null)
        {
            this.AddDiagnostic(
                IrSeverity.Warning,
                $"Unsupported binary operator: {bin.Kind()}",
                bin.GetLocation());
            return this.AllocateRegister();
        }

        var leftReg = this.GenerateExpression(bin.Left);
        var rightReg = this.GenerateExpression(bin.Right);
        var destReg = this.AllocateRegister();

        this.Emit(new IrInstruction
        {
            OpCode = opCode.Value,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Reg(leftReg),
            Right = IrOperand.Reg(rightReg),
            SourceLine = GetLine(bin),
        });
        return destReg;
    }

    private int GenerateLogicalAnd(BinaryExpressionSyntax bin)
    {
        var falseLabel = this.NewLabel("and_false");
        var endLabel = this.NewLabel("and_end");
        var resultReg = this.AllocateRegister();

        var leftReg = this.GenerateExpression(bin.Left);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfZero,
            Left = IrOperand.Reg(leftReg),
            Right = IrOperand.Lbl(falseLabel),
        });

        var rightReg = this.GenerateExpression(bin.Right);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfZero,
            Left = IrOperand.Reg(rightReg),
            Right = IrOperand.Lbl(falseLabel),
        });

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Imm(1),
        });
        this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(endLabel) });

        this.EmitLabel(falseLabel);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Imm(0),
        });

        this.EmitLabel(endLabel);
        return resultReg;
    }

    private int GenerateLogicalOr(BinaryExpressionSyntax bin)
    {
        var trueLabel = this.NewLabel("or_true");
        var endLabel = this.NewLabel("or_end");
        var resultReg = this.AllocateRegister();

        var leftReg = this.GenerateExpression(bin.Left);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfNotZero,
            Left = IrOperand.Reg(leftReg),
            Right = IrOperand.Lbl(trueLabel),
        });

        var rightReg = this.GenerateExpression(bin.Right);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfNotZero,
            Left = IrOperand.Reg(rightReg),
            Right = IrOperand.Lbl(trueLabel),
        });

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Imm(0),
        });
        this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(endLabel) });

        this.EmitLabel(trueLabel);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Imm(1),
        });

        this.EmitLabel(endLabel);
        return resultReg;
    }

    private int GeneratePrefixUnary(PrefixUnaryExpressionSyntax unary)
    {
        if (unary.IsKind(SyntaxKind.PreIncrementExpression) || unary.IsKind(SyntaxKind.PreDecrementExpression))
        {
            return this.GeneratePreIncDec(unary);
        }

        var operandReg = this.GenerateExpression(unary.Operand);
        var destReg = this.AllocateRegister();

        switch (unary.Kind())
        {
            case SyntaxKind.UnaryMinusExpression:
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Neg,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(operandReg),
                    SourceLine = GetLine(unary),
                });
                break;

            case SyntaxKind.BitwiseNotExpression:
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Not,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(operandReg),
                    SourceLine = GetLine(unary),
                });
                break;

            case SyntaxKind.LogicalNotExpression:
                // !x → x == 0
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.CmpEq,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(operandReg),
                    Right = IrOperand.Imm(0),
                    SourceLine = GetLine(unary),
                });
                break;

            default:
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Copy,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(operandReg),
                });
                break;
        }

        return destReg;
    }

    private int GeneratePreIncDec(PrefixUnaryExpressionSyntax unary)
    {
        if (unary.Operand is IdentifierNameSyntax id && this.symbols.TryGetValue(id.Identifier.Text, out var sym))
        {
            var oneReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadImm,
                Dest = IrOperand.Reg(oneReg),
                Left = IrOperand.Imm(1),
            });

            var op = unary.IsKind(SyntaxKind.PreIncrementExpression) ? IrOpCode.Add : IrOpCode.Sub;
            this.Emit(new IrInstruction
            {
                OpCode = op,
                Dest = IrOperand.Reg(sym.Register),
                Left = IrOperand.Reg(sym.Register),
                Right = IrOperand.Reg(oneReg),
                SourceLine = GetLine(unary),
            });

            return sym.Register;
        }

        return this.AllocateRegister();
    }

    private int GeneratePostfixUnary(PostfixUnaryExpressionSyntax postfix)
    {
        if (postfix.Operand is IdentifierNameSyntax id && this.symbols.TryGetValue(id.Identifier.Text, out var sym))
        {
            // Save original value
            var originalReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.Copy,
                Dest = IrOperand.Reg(originalReg),
                Left = IrOperand.Reg(sym.Register),
            });

            var oneReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadImm,
                Dest = IrOperand.Reg(oneReg),
                Left = IrOperand.Imm(1),
            });

            var op = postfix.IsKind(SyntaxKind.PostIncrementExpression) ? IrOpCode.Add : IrOpCode.Sub;
            this.Emit(new IrInstruction
            {
                OpCode = op,
                Dest = IrOperand.Reg(sym.Register),
                Left = IrOperand.Reg(sym.Register),
                Right = IrOperand.Reg(oneReg),
                SourceLine = GetLine(postfix),
            });

            return originalReg;
        }

        return this.AllocateRegister();
    }

    private int GenerateAssignment(AssignmentExpressionSyntax assign)
    {
        if (assign.Left is IdentifierNameSyntax id && this.symbols.TryGetValue(id.Identifier.Text, out var sym))
        {
            if (assign.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                var valueReg = this.GenerateExpression(assign.Right);
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Copy,
                    Dest = IrOperand.Reg(sym.Register),
                    Left = IrOperand.Reg(valueReg),
                    SourceLine = GetLine(assign),
                });
                return sym.Register;
            }

            // Compound assignment: +=, -=, *=, /=, %=
            var compoundOp = assign.Kind() switch
            {
                SyntaxKind.AddAssignmentExpression => IrOpCode.Add,
                SyntaxKind.SubtractAssignmentExpression => IrOpCode.Sub,
                SyntaxKind.MultiplyAssignmentExpression => IrOpCode.Mul,
                SyntaxKind.DivideAssignmentExpression => IrOpCode.Div,
                SyntaxKind.ModuloAssignmentExpression => IrOpCode.Mod,
                SyntaxKind.AndAssignmentExpression => IrOpCode.And,
                SyntaxKind.OrAssignmentExpression => IrOpCode.Or,
                SyntaxKind.ExclusiveOrAssignmentExpression => IrOpCode.Xor,
                SyntaxKind.LeftShiftAssignmentExpression => IrOpCode.Shl,
                SyntaxKind.RightShiftAssignmentExpression => IrOpCode.Shr,
                _ => (IrOpCode?)null,
            };

            if (compoundOp is not null)
            {
                var valueReg = this.GenerateExpression(assign.Right);
                this.Emit(new IrInstruction
                {
                    OpCode = compoundOp.Value,
                    Dest = IrOperand.Reg(sym.Register),
                    Left = IrOperand.Reg(sym.Register),
                    Right = IrOperand.Reg(valueReg),
                    SourceLine = GetLine(assign),
                });
                return sym.Register;
            }
        }

        // Implicit this.field assignment (e.g. Width = 5 inside instance method/ctor)
        if (assign.Left is IdentifierNameSyntax fieldId
            && !this.symbols.ContainsKey(fieldId.Identifier.Text)
            && assign.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && this.currentClassName is not null)
        {
            // Instance field or auto-property via this pointer
            if (this.symbols.TryGetValue("this", out var thisParam)
                && this.TryGetField(this.currentClassName, fieldId.Identifier.Text, out var fieldInfo))
            {
                var valueReg = this.GenerateExpression(assign.Right);
                var addrReg = this.ComputeFieldAddress(thisParam.Register, isPointer: true, fieldInfo.Offset, GetLine(assign));
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.StoreMem,
                    Dest = IrOperand.Reg(addrReg),
                    Left = IrOperand.Reg(valueReg),
                    SourceLine = GetLine(assign),
                });
                return valueReg;
            }

            // Implicit static field (e.g. Count = 0 inside Counter.Reset())
            var fieldSymInfo = this.model?.GetSymbolInfo(fieldId);
            if (fieldSymInfo?.Symbol is IFieldSymbol { IsStatic: true } staticField)
            {
                var key = $"{staticField.ContainingType.Name}.{staticField.Name}";
                if (!this.symbols.TryGetValue(key, out var existing))
                {
                    var reg = this.AllocateRegister();
                    existing = (reg, "int");
                    this.symbols[key] = existing;
                }

                var valueReg = this.GenerateExpression(assign.Right);
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.Copy,
                    Dest = IrOperand.Reg(existing.Register),
                    Left = IrOperand.Reg(valueReg),
                    SourceLine = GetLine(assign),
                });
                return existing.Register;
            }
        }

        // Explicit member assignment (e.g. r.Width = 5 or t.Celsius = 0)
        if (assign.Left is MemberAccessExpressionSyntax memberLhs
            && assign.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            var lhsSymbolInfo = this.model?.GetSymbolInfo(memberLhs);

            // Instance field
            if (lhsSymbolInfo?.Symbol is IFieldSymbol { IsStatic: false } lhsField
                && this.TryGetField(lhsField.ContainingType.Name, lhsField.Name, out var lhsFieldInfo))
            {
                var valueReg = this.GenerateExpression(assign.Right);
                var objReg = this.GenerateExpression(memberLhs.Expression);
                var isPointer = this.IsPointerExpression(memberLhs.Expression);
                var addrReg = this.ComputeFieldAddress(objReg, isPointer, lhsFieldInfo.Offset, GetLine(assign));
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.StoreMem,
                    Dest = IrOperand.Reg(addrReg),
                    Left = IrOperand.Reg(valueReg),
                    SourceLine = GetLine(assign),
                });
                return valueReg;
            }

            // Auto-property (backed by field in layout)
            if (lhsSymbolInfo?.Symbol is IPropertySymbol lhsProp
                && this.TryGetField(lhsProp.ContainingType.Name, lhsProp.Name, out var lhsPropFieldInfo))
            {
                var valueReg = this.GenerateExpression(assign.Right);
                var objReg = this.GenerateExpression(memberLhs.Expression);
                var isPointer = this.IsPointerExpression(memberLhs.Expression);
                var addrReg = this.ComputeFieldAddress(objReg, isPointer, lhsPropFieldInfo.Offset, GetLine(assign));
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.StoreMem,
                    Dest = IrOperand.Reg(addrReg),
                    Left = IrOperand.Reg(valueReg),
                    SourceLine = GetLine(assign),
                });
                return valueReg;
            }
        }

        this.AddDiagnostic(
            IrSeverity.Warning,
            $"Unsupported assignment target: {assign.Left}",
            assign.GetLocation());
        return this.AllocateRegister();
    }

    private int GenerateMemberAccess(MemberAccessExpressionSyntax memberAccess)
    {
        // Resolve enum or constant values via the semantic model
        var constValue = this.model?.GetConstantValue(memberAccess);
        if (constValue is { HasValue: true, Value: int enumVal })
        {
            var reg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadImm,
                Dest = IrOperand.Reg(reg),
                Left = IrOperand.Imm(enumVal),
                SourceLine = GetLine(memberAccess),
            });
            return reg;
        }

        var symbolInfo = this.model?.GetSymbolInfo(memberAccess);

        // Static field access (e.g. Counter.Count)
        if (symbolInfo?.Symbol is IFieldSymbol { IsStatic: true } field)
        {
            var key = $"{field.ContainingType.Name}.{field.Name}";
            if (!this.symbols.TryGetValue(key, out var existing))
            {
                var reg = this.AllocateRegister();
                existing = (reg, "int");
                this.symbols[key] = existing;
            }

            return existing.Register;
        }

        // Instance field access (e.g. r.Width, this.Width, other.X)
        if (symbolInfo?.Symbol is IFieldSymbol { IsStatic: false } instanceField
            && this.TryGetField(instanceField.ContainingType.Name, instanceField.Name, out var instFieldInfo))
        {
            var objReg = this.GenerateExpression(memberAccess.Expression);
            var isPointer = this.IsPointerExpression(memberAccess.Expression);
            var addrReg = this.ComputeFieldAddress(objReg, isPointer, instFieldInfo.Offset, GetLine(memberAccess));
            var destReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadMem,
                Dest = IrOperand.Reg(destReg),
                Left = IrOperand.Reg(addrReg),
                SourceLine = GetLine(memberAccess),
            });
            return destReg;
        }

        // Property access (e.g. t.Celsius, t.Fahrenheit)
        if (symbolInfo?.Symbol is IPropertySymbol propSymbol)
        {
            var className = propSymbol.ContainingType.Name;

            // Auto-property → backed by a field in the layout
            if (this.TryGetField(className, propSymbol.Name, out var propFieldInfo))
            {
                var objReg = this.GenerateExpression(memberAccess.Expression);
                var isPointer = this.IsPointerExpression(memberAccess.Expression);
                var addrReg = this.ComputeFieldAddress(objReg, isPointer, propFieldInfo.Offset, GetLine(memberAccess));
                var destReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadMem,
                    Dest = IrOperand.Reg(destReg),
                    Left = IrOperand.Reg(addrReg),
                    SourceLine = GetLine(memberAccess),
                });
                return destReg;
            }

            // Computed property → call getter method
            return this.GenerateInstanceCall(
                memberAccess.Expression,
                $"{className}_get_{propSymbol.Name}",
                GetLine(memberAccess));
        }

        this.AddDiagnostic(
            IrSeverity.Warning,
            $"Unsupported member access: {memberAccess}",
            memberAccess.GetLocation());
        return this.AllocateRegister();
    }

    private int GenerateConditional(ConditionalExpressionSyntax cond)
    {
        var condReg = this.GenerateExpression(cond.Condition);
        var elseLabel = this.NewLabel("cond_else");
        var endLabel = this.NewLabel("cond_end");
        var resultReg = this.AllocateRegister();

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.JumpIfZero,
            Left = IrOperand.Reg(condReg),
            Right = IrOperand.Lbl(elseLabel),
        });

        var thenReg = this.GenerateExpression(cond.WhenTrue);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Reg(thenReg),
        });
        this.Emit(new IrInstruction { OpCode = IrOpCode.Jump, Left = IrOperand.Lbl(endLabel) });

        this.EmitLabel(elseLabel);
        var elseReg = this.GenerateExpression(cond.WhenFalse);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Copy,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Reg(elseReg),
        });

        this.EmitLabel(endLabel);
        return resultReg;
    }

    private int GenerateDummy(ExpressionSyntax expr)
    {
        this.AddDiagnostic(
            IrSeverity.Warning,
            $"Unsupported expression: {expr.Kind()} — emitting zero",
            expr.GetLocation());
        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Imm(0),
        });
        return reg;
    }

    // ────────────────────────────────────────────────────────
    //  Object creation (new ClassName(...))
    // ────────────────────────────────────────────────────────
    private int GenerateObjectCreation(ObjectCreationExpressionSyntax creation)
    {
        var typeInfo = this.model?.GetTypeInfo(creation);
        var className = typeInfo?.Type?.Name;

        if (className is null || !this.classLayouts.TryGetValue(className, out var fields))
        {
            return this.GenerateDummy(creation);
        }

        var totalSize = fields.Sum(f => f.Size);
        if (totalSize < 2)
        {
            totalSize = 2;
        }

        // Allocate struct-sized register for the new object
        var objReg = this.AllocateRegister();
        this.registerSizes[objReg] = totalSize;

        this.EmitConstructorCall(creation, objReg);
        return objReg;
    }

    /// <summary>
    /// Initialises the object at <paramref name="targetReg"/> by calling the constructor.
    /// Used when the target register is already allocated (e.g. a local variable).
    /// </summary>
    private void GenerateObjectCreationInto(ObjectCreationExpressionSyntax creation, int targetReg)
    {
        this.EmitConstructorCall(creation, targetReg);
    }

    private void EmitConstructorCall(ObjectCreationExpressionSyntax creation, int targetReg)
    {
        var typeInfo = this.model?.GetTypeInfo(creation);
        var className = typeInfo?.Type?.Name ?? "Unknown";

        // Get address of the target object's storage
        var addrReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadAddress,
            Dest = IrOperand.Reg(addrReg),
            Left = IrOperand.Reg(targetReg),
            SourceLine = GetLine(creation),
        });

        // Push arguments right-to-left, then push the 'this' pointer
        var args = creation.ArgumentList?.Arguments ?? default;
        for (var i = args.Count - 1; i >= 0; i--)
        {
            var argReg = this.GenerateExpression(args[i].Expression);
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.Push,
                Left = IrOperand.Reg(argReg),
                SourceLine = GetLine(creation),
            });
        }

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Push,
            Left = IrOperand.Reg(addrReg),
            SourceLine = GetLine(creation),
        });

        var resultReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Call,
            Dest = IrOperand.Reg(resultReg),
            Left = IrOperand.Lbl($"{className}_ctor"),
            Right = IrOperand.Imm(args.Count + 1),
            SourceLine = GetLine(creation),
        });
    }

    // ────────────────────────────────────────────────────────
    //  Invocations (method calls)
    // ────────────────────────────────────────────────────────
    private int GenerateInvocation(InvocationExpressionSyntax invoke)
    {
        // Console.Write / Console.WriteLine
        if (invoke.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var objText = memberAccess.Expression.ToString();
            var methodName = memberAccess.Name.Identifier.Text;

            if (objText == "Console")
            {
                return this.GenerateConsolePrint(methodName, invoke.ArgumentList, invoke);
            }

            var symbolInfo = this.model?.GetSymbolInfo(invoke);
            if (symbolInfo?.Symbol is IMethodSymbol methodSym)
            {
                // Instance method call (e.g. r.Area())
                if (!methodSym.IsStatic)
                {
                    return this.GenerateInstanceCall(memberAccess.Expression, methodSym, invoke.ArgumentList, invoke);
                }

                return this.GenerateResolvedCall(methodSym, invoke.ArgumentList, invoke);
            }
        }

        // Simple function call (e.g. MyMethod(args))
        if (invoke.Expression is IdentifierNameSyntax funcName)
        {
            return this.GenerateUserCall(funcName.Identifier.Text, invoke.ArgumentList, invoke);
        }

        return this.AllocateRegister();
    }

    private int GenerateInstanceCall(
        ExpressionSyntax receiver,
        IMethodSymbol method,
        ArgumentListSyntax args,
        SyntaxNode node)
    {
        var className = method.ContainingType.Name;
        var objReg = this.GenerateExpression(receiver);

        // Determine whether objReg is already a pointer (this) or a local struct
        var isPointer = receiver is ThisExpressionSyntax;
        int addrReg;

        if (isPointer)
        {
            addrReg = objReg;
        }
        else
        {
            addrReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadAddress,
                Dest = IrOperand.Reg(addrReg),
                Left = IrOperand.Reg(objReg),
                SourceLine = GetLine(node),
            });
        }

        // Push arguments right-to-left
        for (var i = args.Arguments.Count - 1; i >= 0; i--)
        {
            var argExpr = args.Arguments[i].Expression;
            var argReg = this.GenerateExpression(argExpr);

            // Struct-typed arguments must be passed by pointer
            var pushReg = argReg;
            if (i < method.Parameters.Length
                && this.classLayouts.ContainsKey(method.Parameters[i].Type.Name)
                && !this.IsPointerExpression(argExpr))
            {
                pushReg = this.AllocateRegister();
                this.Emit(new IrInstruction
                {
                    OpCode = IrOpCode.LoadAddress,
                    Dest = IrOperand.Reg(pushReg),
                    Left = IrOperand.Reg(argReg),
                    SourceLine = GetLine(node),
                });
            }

            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.Push,
                Left = IrOperand.Reg(pushReg),
                SourceLine = GetLine(node),
            });
        }

        // Push 'this' pointer as the first argument
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Push,
            Left = IrOperand.Reg(addrReg),
            SourceLine = GetLine(node),
        });

        var funcName = $"{className}_{method.Name}";
        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Call,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Lbl(funcName),
            Right = IrOperand.Imm(args.Arguments.Count + 1),
            SourceLine = GetLine(node),
        });

        return destReg;
    }

    private int GenerateInstanceCall(ExpressionSyntax receiver, string funcName, int sourceLine)
    {
        var objReg = this.GenerateExpression(receiver);
        var isPointer = receiver is ThisExpressionSyntax;
        int addrReg;

        if (isPointer)
        {
            addrReg = objReg;
        }
        else
        {
            addrReg = this.AllocateRegister();
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.LoadAddress,
                Dest = IrOperand.Reg(addrReg),
                Left = IrOperand.Reg(objReg),
                SourceLine = sourceLine,
            });
        }

        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Push,
            Left = IrOperand.Reg(addrReg),
            SourceLine = sourceLine,
        });

        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Call,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Lbl(funcName),
            Right = IrOperand.Imm(1),
            SourceLine = sourceLine,
        });

        return destReg;
    }

    private int GenerateResolvedCall(IMethodSymbol method, ArgumentListSyntax args, SyntaxNode node)
    {
        var funcName = method.Name == "Main" ? "main" : method.Name;
        return this.GenerateUserCall(funcName, args, node);
    }

    private int GenerateUserCall(string funcName, ArgumentListSyntax args, SyntaxNode node)
    {
        // Push arguments right-to-left (C calling convention)
        for (var i = args.Arguments.Count - 1; i >= 0; i--)
        {
            var argReg = this.GenerateExpression(args.Arguments[i].Expression);
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.Push,
                Left = IrOperand.Reg(argReg),
                SourceLine = GetLine(node),
            });
        }

        var destReg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.Call,
            Dest = IrOperand.Reg(destReg),
            Left = IrOperand.Lbl(funcName),
            Right = IrOperand.Imm(args.Arguments.Count),
            SourceLine = GetLine(node),
        });

        return destReg;
    }

    // ────────────────────────────────────────────────────────
    //  Console.Write / Console.WriteLine expansion
    // ────────────────────────────────────────────────────────
    private int GenerateConsolePrint(string methodName, ArgumentListSyntax args, SyntaxNode node)
    {
        var isWriteLine = methodName == "WriteLine";

        if (args.Arguments.Count == 0)
        {
            // Console.WriteLine() → just newline
            if (isWriteLine)
            {
                this.EmitNewline(GetLine(node));
            }

            return this.AllocateRegister();
        }

        var arg = args.Arguments[0].Expression;

        // Expand the argument as a print expression (handles string concat)
        this.EmitPrintExpression(arg);

        if (isWriteLine)
        {
            this.EmitNewline(GetLine(node));
        }

        return this.AllocateRegister();
    }

    /// <summary>
    /// Recursively expands an expression into print instructions.
    /// Handles string concatenation chains by splitting into segments.
    /// </summary>
    private void EmitPrintExpression(ExpressionSyntax expr)
    {
        // Parenthesized → unwrap
        if (expr is ParenthesizedExpressionSyntax paren)
        {
            this.EmitPrintExpression(paren.Expression);
            return;
        }

        // String concatenation: "text" + expr or expr + "text"
        if (expr is BinaryExpressionSyntax bin && bin.IsKind(SyntaxKind.AddExpression))
        {
            var typeInfo = this.model?.GetTypeInfo(expr);
            if (typeInfo?.Type?.SpecialType == SpecialType.System_String)
            {
                // This is string concatenation → flatten and print each part
                this.EmitPrintExpression(bin.Left);
                this.EmitPrintExpression(bin.Right);
                return;
            }
        }

        // String literal → PrintStr
        if (expr is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
        {
            var text = lit.Token.ValueText;
            if (text.Length > 0)
            {
                this.EmitPrintStr(text, GetLine(lit));
            }

            return;
        }

        // Determine expression type from the semantic model
        var exprTypeInfo = this.model?.GetTypeInfo(expr);
        var specialType = exprTypeInfo?.Type?.SpecialType;

        if (specialType == SpecialType.System_Char)
        {
            var reg = this.GenerateExpression(expr);
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.PrintChar,
                Left = IrOperand.Reg(reg),
                SourceLine = GetLine(expr),
            });
            return;
        }

        if (specialType == SpecialType.System_String)
        {
            // Non-literal string expression → evaluate and print (pointer)
            var reg = this.GenerateExpression(expr);
            this.Emit(new IrInstruction
            {
                OpCode = IrOpCode.PrintStr,
                Left = IrOperand.Reg(reg),
                SourceLine = GetLine(expr),
            });
            return;
        }

        // Default: treat as integer and use PrintInt
        var intReg = this.GenerateExpression(expr);
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.PrintInt,
            Left = IrOperand.Reg(intReg),
            SourceLine = GetLine(expr),
        });
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

    private void EmitNewline(int sourceLine)
    {
        var reg = this.AllocateRegister();
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.LoadImm,
            Dest = IrOperand.Reg(reg),
            Left = IrOperand.Imm('\n'),
            SourceLine = sourceLine,
        });
        this.Emit(new IrInstruction
        {
            OpCode = IrOpCode.PrintChar,
            Left = IrOperand.Reg(reg),
            SourceLine = sourceLine,
        });
    }

    // ────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────
    private int AllocateRegister() => this.registerCount++;

    private string NewLabel(string prefix) => $"_{prefix}_{this.labelCounter++}";

    private void Emit(IrInstruction instruction) => this.instructions.Add(instruction);

    private void EmitLabel(string name) =>
        this.Emit(new IrInstruction { OpCode = IrOpCode.Label, Left = IrOperand.Lbl(name) });

    private bool IsPointerExpression(ExpressionSyntax expr)
    {
        if (expr is ThisExpressionSyntax)
        {
            return true;
        }

        if (expr is IdentifierNameSyntax id
            && this.symbols.TryGetValue(id.Identifier.Text, out var sym))
        {
            return sym.TypeName.EndsWith('*');
        }

        return false;
    }

    private void AddDiagnostic(Pipeline.Ir.DiagnosticSeverity severity, string message, Location location)
    {
        var lineSpan = location.GetLineSpan();
        this.diagnostics.Add(new IrDiagnostic
        {
            Severity = severity,
            Message = message,
            Line = lineSpan.StartLinePosition.Line + 1,
            Column = lineSpan.StartLinePosition.Character + 1,
            SourceFile = lineSpan.Path,
        });
    }
}
