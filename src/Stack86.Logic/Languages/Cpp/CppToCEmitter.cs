namespace Stack86.Logic.Languages.Cpp;

using System.Text;
using Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// Transpiles a C++ AST into equivalent C source code.
/// Applies transformations for classes, references, cout/cin, new/delete, etc.
/// </summary>
public sealed class CppToCEmitter
{
    private readonly StringBuilder output = new();
    private readonly StringBuilder forwardDeclarations = new();
    private readonly StringBuilder structDefinitions = new();
    private readonly StringBuilder functionDefinitions = new();
    private readonly Dictionary<string, CppClassInfo> classRegistry = [];
    private readonly Dictionary<string, List<bool>> functionRefParams = [];
    private readonly Dictionary<string, List<CppExpressionNode?>> functionDefaults = [];
    private readonly Dictionary<string, string> variableTypes = [];
    private readonly HashSet<string> referenceParameters = [];
    private readonly List<CppLineMapping> lineMappings = [];
    private string? currentClassName;
    private int indentLevel;

    /// <summary>
    /// Gets the line mappings from output C lines to original C++ lines.
    /// </summary>
    public IReadOnlyList<CppLineMapping> LineMappings => this.lineMappings;

    /// <summary>
    /// Gets a value indicating whether the emitted C code requires <c>stdlib.h</c>
    /// (e.g. because <c>new</c>/<c>delete</c> was transpiled to <c>malloc</c>/<c>free</c>).
    /// </summary>
    public bool NeedsStdlib { get; private set; }

    /// <summary>
    /// Emits C source code from a C++ AST program node.
    /// </summary>
    /// <param name="program">The C++ AST root.</param>
    /// <returns>The transpiled C source code.</returns>
    public string Emit(CppProgramNode program)
    {
        // First pass: collect class info
        this.CollectClassInfo(program.Declarations);

        // Second pass: emit C code
        foreach (var decl in program.Declarations)
        {
            this.EmitDeclaration(decl);
        }

        // Assemble final output: includes, forward decls, structs, then functions
        this.output.Clear();
        this.output.Append(this.forwardDeclarations);
        this.output.AppendLine();
        this.output.Append(this.structDefinitions);
        this.output.AppendLine();
        this.output.Append(this.functionDefinitions);

        return this.output.ToString();
    }

    // ────────────────────────────────────────────────────────
    //  Private static helpers
    // ────────────────────────────────────────────────────────
    private static string TypeToFormatSpec(string typeStr)
    {
        return typeStr switch
        {
            "int" => "%d",
            "char" => "%c",
            "char*" => "%s",
            _ => "%d",
        };
    }

    private static string EscapeForPrintf(string str)
    {
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\r", "\\r")
            .Replace("%", "%%");
    }

    private static string FormatCharLiteral(char c)
    {
        return c switch
        {
            '\n' => "'\\n'",
            '\t' => "'\\t'",
            '\r' => "'\\r'",
            '\0' => "'\\0'",
            '\\' => "'\\\\'",
            '\'' => "'\\''",
            _ => $"'{c}'",
        };
    }

    private static string PrimitiveToString(CppPrimitiveType prim)
    {
        var prefix = prim.IsUnsigned ? "unsigned " : string.Empty;
        return prim.Kind switch
        {
            CppPrimitiveKind.Int => prefix + "int",
            CppPrimitiveKind.Char => prefix + "char",
            CppPrimitiveKind.Void => "void",
            CppPrimitiveKind.Bool => "int",          // bool → int
            CppPrimitiveKind.Short => prefix + "short",
            CppPrimitiveKind.Long => prefix + "long",
            _ => "int",
        };
    }

    private static string EscapeForC(string str)
    {
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\r", "\\r");
    }

    // ────────────────────────────────────────────────────────
    //  Class info collection (first pass)
    // ────────────────────────────────────────────────────────
    private void CollectClassInfo(IReadOnlyList<CppDeclarationNode> declarations)
    {
        foreach (var decl in declarations)
        {
            if (decl is CppClassDeclaration cls)
            {
                this.RegisterClass(cls);
            }
            else if (decl is CppFunctionDeclaration func)
            {
                this.RegisterFunctionRefParams(func.Name, func.Parameters);
            }
            else if (decl is CppNamespaceDeclaration ns)
            {
                this.CollectClassInfo(ns.Declarations);
            }
            else if (decl is CppTemplateDeclaration tmpl && tmpl.Inner is CppClassDeclaration innerCls)
            {
                this.RegisterClass(innerCls);
            }
        }
    }

    private void RegisterFunctionRefParams(string name, IReadOnlyList<CppParameterDeclaration> parameters)
    {
        var refFlags = parameters.Select(p => p.Type is CppReferenceType).ToList();
        if (refFlags.Exists(f => f))
        {
            this.functionRefParams[name] = refFlags;
        }

        var defaults = parameters.Select(p => p.DefaultValue).ToList();
        if (defaults.Exists(d => d is not null))
        {
            this.functionDefaults[name] = defaults;
        }
    }

    private void RegisterClass(CppClassDeclaration cls)
    {
        var info = new CppClassInfo { Name = cls.Name, BaseClassName = cls.BaseClassName };

        foreach (var section in cls.Sections)
        {
            foreach (var member in section.Members)
            {
                if (member is CppFieldDeclaration field)
                {
                    info.Fields.Add((field.Name, this.TypeToString(field.Type)));
                }
                else if (member is CppMethodDeclaration method)
                {
                    info.Methods.Add(method.Name);
                    var mangledName = CppNameMangler.MangleMethod(cls.Name, method.Name);
                    this.RegisterFunctionRefParams(mangledName, method.Parameters);
                }
                else if (member is CppConstructorDeclaration ctor)
                {
                    var mangledCtor = CppNameMangler.MangleConstructor(cls.Name);
                    this.RegisterFunctionRefParams(mangledCtor, ctor.Parameters);
                }
            }
        }

        this.classRegistry[cls.Name] = info;
    }

    // ────────────────────────────────────────────────────────
    //  Declaration emission
    // ────────────────────────────────────────────────────────
    private void EmitDeclaration(CppDeclarationNode decl)
    {
        switch (decl)
        {
            case CppUsingDirective:
                // using namespace std; → no output (iostream mapped to stdio.h by CppIncludeMapper)
                break;

            case CppNamespaceDeclaration ns:
                this.EmitNamespace(ns);
                break;

            case CppClassDeclaration cls:
                this.EmitClass(cls);
                break;

            case CppFunctionDeclaration func:
                this.EmitFunction(func);
                break;

            case CppVariableDeclaration varDecl:
                this.EmitGlobalVariable(varDecl);
                break;

            case CppTemplateDeclaration:
                // Templates are emitted on-demand at instantiation sites (future enhancement)
                // For now, emit the inner declaration with the template param replaced
                break;

            default:
                break;
        }
    }

    // ────────────────────────────────────────────────────────
    //  Namespace
    // ────────────────────────────────────────────────────────
    private void EmitNamespace(CppNamespaceDeclaration ns)
    {
        // Namespaces → prefix mangling, just emit contents with mangled names
        // For simplicity, we emit the contents directly (using namespace resolves names)
        foreach (var decl in ns.Declarations)
        {
            this.EmitDeclaration(decl);
        }
    }

    // ────────────────────────────────────────────────────────
    //  Class → struct + free functions
    // ────────────────────────────────────────────────────────
    private void EmitClass(CppClassDeclaration cls)
    {
        var name = cls.Name;

        // Forward declaration
        this.forwardDeclarations.AppendLine($"struct {name};");

        // Struct definition
        this.structDefinitions.AppendLine($"struct {name} {{");

        // Inheritance: embed base struct as first field
        if (cls.BaseClassName is not null)
        {
            this.structDefinitions.AppendLine($"    struct {cls.BaseClassName} base;");
        }

        // Emit fields from all sections
        foreach (var section in cls.Sections)
        {
            foreach (var member in section.Members)
            {
                if (member is CppFieldDeclaration field)
                {
                    this.structDefinitions.AppendLine($"    {this.TypeToString(field.Type)} {field.Name};");
                }
            }
        }

        this.structDefinitions.AppendLine("};");
        this.structDefinitions.AppendLine();

        // Emit methods, constructors, destructors as free functions
        this.currentClassName = name;
        foreach (var section in cls.Sections)
        {
            foreach (var member in section.Members)
            {
                switch (member)
                {
                    case CppConstructorDeclaration ctor:
                        this.EmitConstructor(name, ctor);
                        break;
                    case CppDestructorDeclaration dtor:
                        this.EmitDestructor(name, dtor);
                        break;
                    case CppMethodDeclaration method:
                        this.EmitMethod(name, method);
                        break;
                }
            }
        }

        this.currentClassName = null;
    }

    private void EmitConstructor(string className, CppConstructorDeclaration ctor)
    {
        var mangledName = CppNameMangler.MangleConstructor(className);
        var paramList = this.BuildMethodParamList(className, ctor.Parameters);

        this.functionDefinitions.AppendLine($"void {mangledName}({paramList}) {{");

        // Member initializer list → assignments or base constructor calls
        foreach (var init in ctor.Initializers)
        {
            if (this.classRegistry.ContainsKey(init.MemberName))
            {
                // Base class constructor call: Base(args) → Base_init(&this_ptr->base, args)
                var baseCtor = CppNameMangler.MangleConstructor(init.MemberName);
                var args = string.Join(", ", init.Arguments.Select(this.ExprToString));
                var argsWithThis = args.Length > 0
                    ? $"(struct {init.MemberName}*)this_ptr, {args}"
                    : $"(struct {init.MemberName}*)this_ptr";
                this.functionDefinitions.AppendLine($"    {baseCtor}({argsWithThis});");
            }
            else if (init.Arguments.Count == 1)
            {
                // Simple field initializer: field(value) → this_ptr->field = value
                this.functionDefinitions.Append("    this_ptr->");
                this.functionDefinitions.Append(init.MemberName);
                this.functionDefinitions.Append(" = ");
                this.functionDefinitions.Append(this.ExprToString(init.Arguments[0]));
                this.functionDefinitions.AppendLine(";");
            }
        }

        // Constructor body
        if (ctor.Body is not null)
        {
            this.EmitBlockContents(ctor.Body, this.functionDefinitions);
        }

        this.functionDefinitions.AppendLine("}");
        this.functionDefinitions.AppendLine();
    }

    private void EmitDestructor(string className, CppDestructorDeclaration dtor)
    {
        var mangledName = CppNameMangler.MangleDestructor(className);

        this.functionDefinitions.AppendLine($"void {mangledName}(struct {className}* this_ptr) {{");

        if (dtor.Body is not null)
        {
            this.EmitBlockContents(dtor.Body, this.functionDefinitions);
        }

        this.functionDefinitions.AppendLine("}");
        this.functionDefinitions.AppendLine();
    }

    private void EmitMethod(string className, CppMethodDeclaration method)
    {
        if (method.Body is null)
        {
            return; // skip declarations without body
        }

        var mangledName = CppNameMangler.MangleMethod(className, method.Name);
        var returnType = this.TypeToString(method.ReturnType);
        var paramList = this.BuildMethodParamList(className, method.Parameters);

        // Forward declare the method
        this.forwardDeclarations.AppendLine($"{returnType} {mangledName}({paramList});");

        // Emit method body
        this.functionDefinitions.AppendLine($"{returnType} {mangledName}({paramList}) {{");

        // Track reference parameters for this scope
        var prevRefParams = new HashSet<string>(this.referenceParameters);
        foreach (var param in method.Parameters)
        {
            this.variableTypes[param.Name] = this.TypeToString(param.Type);
            if (param.Type is CppReferenceType)
            {
                this.referenceParameters.Add(param.Name);
            }
        }

        this.EmitBlockContents(method.Body, this.functionDefinitions);

        this.referenceParameters.Clear();
        foreach (var rp in prevRefParams)
        {
            this.referenceParameters.Add(rp);
        }

        this.functionDefinitions.AppendLine("}");
        this.functionDefinitions.AppendLine();
    }

    private string BuildMethodParamList(string className, IReadOnlyList<CppParameterDeclaration> parameters)
    {
        var parts = new List<string> { $"struct {className}* this_ptr" };
        foreach (var param in parameters)
        {
            var typeStr = this.TypeToString(param.Type);
            if (param.Type is CppReferenceType refType)
            {
                // Reference → pointer
                typeStr = this.TypeToString(refType.Inner) + "*";
            }

            parts.Add($"{typeStr} {param.Name}");
        }

        return string.Join(", ", parts);
    }

    // ────────────────────────────────────────────────────────
    //  Free function
    // ────────────────────────────────────────────────────────
    private void EmitFunction(CppFunctionDeclaration func)
    {
        var returnType = this.TypeToString(func.ReturnType);
        var paramList = this.BuildFreeParamList(func.Parameters);

        // Forward declare
        this.forwardDeclarations.AppendLine($"{returnType} {func.Name}({paramList});");

        this.functionDefinitions.AppendLine($"{returnType} {func.Name}({paramList}) {{");

        // Track reference parameters
        var prevRefParams = new HashSet<string>(this.referenceParameters);
        foreach (var param in func.Parameters)
        {
            this.variableTypes[param.Name] = this.TypeToString(param.Type);
            if (param.Type is CppReferenceType)
            {
                this.referenceParameters.Add(param.Name);
            }
        }

        if (func.Body is not null)
        {
            this.EmitBlockContents(func.Body, this.functionDefinitions);
        }

        this.referenceParameters.Clear();
        foreach (var rp in prevRefParams)
        {
            this.referenceParameters.Add(rp);
        }

        this.functionDefinitions.AppendLine("}");
        this.functionDefinitions.AppendLine();
    }

    private string BuildFreeParamList(IReadOnlyList<CppParameterDeclaration> parameters)
    {
        if (parameters.Count == 0)
        {
            return "void";
        }

        var parts = new List<string>();
        foreach (var param in parameters)
        {
            var typeStr = this.TypeToString(param.Type);
            if (param.Type is CppReferenceType refType)
            {
                typeStr = this.TypeToString(refType.Inner) + "*";
            }

            parts.Add($"{typeStr} {param.Name}");
        }

        return string.Join(", ", parts);
    }

    // ────────────────────────────────────────────────────────
    //  Global variable
    // ────────────────────────────────────────────────────────
    private void EmitGlobalVariable(CppVariableDeclaration decl)
    {
        var typeStr = this.TypeToString(decl.Type);
        var line = $"{typeStr} {decl.Name}";
        if (decl.Initializer is not null)
        {
            line += " = " + this.ExprToString(decl.Initializer);
        }

        this.functionDefinitions.AppendLine(line + ";");
    }

    // ────────────────────────────────────────────────────────
    //  Statements
    // ────────────────────────────────────────────────────────
    private void EmitBlockContents(CppBlockStatement block, StringBuilder sb)
    {
        this.indentLevel++;
        foreach (var stmt in block.Statements)
        {
            this.EmitStatement(stmt, sb);
        }

        this.indentLevel--;
    }

    private void EmitStatement(CppStatementNode stmt, StringBuilder sb)
    {
        var indent = new string(' ', this.indentLevel * 4);

        switch (stmt)
        {
            case CppBlockStatement block:
                sb.AppendLine($"{indent}{{");
                this.EmitBlockContents(block, sb);
                sb.AppendLine($"{indent}}}");
                break;

            case CppReturnStatement ret:
                if (ret.Value is not null)
                {
                    sb.AppendLine($"{indent}return {this.ExprToString(ret.Value)};");
                }
                else
                {
                    sb.AppendLine($"{indent}return;");
                }

                break;

            case CppExpressionStatement exprStmt:
                sb.AppendLine($"{indent}{this.ExprToString(exprStmt.Expression)};");
                break;

            case CppVariableDeclarationStatement varStmt:
                this.EmitVarDeclStatement(varStmt.Declaration, sb, indent);
                break;

            case CppIfStatement ifStmt:
                sb.AppendLine($"{indent}if ({this.ExprToString(ifStmt.Condition)}) {{");
                if (ifStmt.Then is CppBlockStatement thenBlock)
                {
                    this.EmitBlockContents(thenBlock, sb);
                }
                else
                {
                    this.indentLevel++;
                    this.EmitStatement(ifStmt.Then, sb);
                    this.indentLevel--;
                }

                if (ifStmt.Else is not null)
                {
                    sb.AppendLine($"{indent}}} else {{");
                    if (ifStmt.Else is CppBlockStatement elseBlock)
                    {
                        this.EmitBlockContents(elseBlock, sb);
                    }
                    else
                    {
                        this.indentLevel++;
                        this.EmitStatement(ifStmt.Else, sb);
                        this.indentLevel--;
                    }
                }

                sb.AppendLine($"{indent}}}");
                break;

            case CppWhileStatement whileStmt:
                sb.AppendLine($"{indent}while ({this.ExprToString(whileStmt.Condition)}) {{");
                if (whileStmt.Body is CppBlockStatement whileBlock)
                {
                    this.EmitBlockContents(whileBlock, sb);
                }
                else
                {
                    this.indentLevel++;
                    this.EmitStatement(whileStmt.Body, sb);
                    this.indentLevel--;
                }

                sb.AppendLine($"{indent}}}");
                break;

            case CppForStatement forStmt:
                this.EmitForStatement(forStmt, sb, indent);
                break;

            case CppDoWhileStatement doStmt:
                sb.AppendLine($"{indent}do {{");
                if (doStmt.Body is CppBlockStatement doBlock)
                {
                    this.EmitBlockContents(doBlock, sb);
                }
                else
                {
                    this.indentLevel++;
                    this.EmitStatement(doStmt.Body, sb);
                    this.indentLevel--;
                }

                sb.AppendLine($"{indent}}} while ({this.ExprToString(doStmt.Condition)});");
                break;

            case CppSwitchStatement switchStmt:
                sb.AppendLine($"{indent}switch ({this.ExprToString(switchStmt.Expression)}) {{");
                foreach (var caseClause in switchStmt.Cases)
                {
                    if (caseClause.Value is not null)
                    {
                        sb.AppendLine($"{indent}case {this.ExprToString(caseClause.Value)}:");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}default:");
                    }

                    this.indentLevel++;
                    foreach (var s in caseClause.Body)
                    {
                        this.EmitStatement(s, sb);
                    }

                    this.indentLevel--;
                }

                sb.AppendLine($"{indent}}}");
                break;

            case CppBreakStatement:
                sb.AppendLine($"{indent}break;");
                break;

            case CppContinueStatement:
                sb.AppendLine($"{indent}continue;");
                break;

            case CppCoutStatement coutStmt:
                this.EmitCoutStatement(coutStmt, sb, indent);
                break;

            case CppCinStatement cinStmt:
                this.EmitCinStatement(cinStmt, sb, indent);
                break;
        }
    }

    private void EmitVarDeclStatement(CppVariableDeclaration decl, StringBuilder sb, string indent)
    {
        var typeStr = this.TypeToString(decl.Type);
        this.variableTypes[decl.Name] = typeStr;

        // Check if this is a class instance construction
        if (decl.Type is CppNamedType namedType && this.classRegistry.ContainsKey(namedType.Name))
        {
            var className = namedType.Name;
            sb.AppendLine($"{indent}struct {className} {decl.Name};");

            // If there's an initializer that's a constructor call
            if (decl.Initializer is CppCallExpression ctorCall)
            {
                var mangledCtor = CppNameMangler.MangleConstructor(className);
                var args = string.Join(", ", ctorCall.Arguments.Select(this.ExprToString));
                var argsWithThis = args.Length > 0 ? $"&{decl.Name}, {args}" : $"&{decl.Name}";
                sb.AppendLine($"{indent}{mangledCtor}({argsWithThis});");
            }
            else if (decl.Initializer is not null)
            {
                sb.AppendLine($"{indent}{decl.Name} = {this.ExprToString(decl.Initializer)};");
            }

            return;
        }

        // Reference variable → pointer
        if (decl.Type is CppReferenceType refType)
        {
            typeStr = this.TypeToString(refType.Inner) + "*";
            this.referenceParameters.Add(decl.Name);
            sb.Append($"{indent}{typeStr} {decl.Name}");
            if (decl.Initializer is not null)
            {
                sb.Append($" = &({this.ExprToString(decl.Initializer)})");
            }

            sb.AppendLine(";");
            return;
        }

        sb.Append($"{indent}{typeStr} {decl.Name}");
        if (decl.Initializer is not null)
        {
            sb.Append($" = {this.ExprToString(decl.Initializer)}");
        }

        sb.AppendLine(";");
    }

    private void EmitForStatement(CppForStatement forStmt, StringBuilder sb, string indent)
    {
        var initStr = string.Empty;
        if (forStmt.Init is CppVariableDeclarationStatement varInit)
        {
            var decl = varInit.Declaration;
            var typeStr = this.TypeToString(decl.Type);
            this.variableTypes[decl.Name] = typeStr;
            initStr = $"{typeStr} {decl.Name}";
            if (decl.Initializer is not null)
            {
                initStr += $" = {this.ExprToString(decl.Initializer)}";
            }
        }
        else if (forStmt.Init is CppExpressionStatement exprInit)
        {
            initStr = this.ExprToString(exprInit.Expression);
        }

        var condStr = forStmt.Condition is not null ? this.ExprToString(forStmt.Condition) : string.Empty;
        var incrStr = forStmt.Increment is not null ? this.ExprToString(forStmt.Increment) : string.Empty;

        sb.AppendLine($"{indent}for ({initStr}; {condStr}; {incrStr}) {{");
        if (forStmt.Body is CppBlockStatement forBlock)
        {
            this.EmitBlockContents(forBlock, sb);
        }
        else
        {
            this.indentLevel++;
            this.EmitStatement(forStmt.Body, sb);
            this.indentLevel--;
        }

        sb.AppendLine($"{indent}}}");
    }

    // ────────────────────────────────────────────────────────
    //  cout / cin → printf / scanf
    // ────────────────────────────────────────────────────────
    private void EmitCoutStatement(CppCoutStatement stmt, StringBuilder sb, string indent)
    {
        foreach (var expr in stmt.Expressions)
        {
            if (expr is CppEndlExpression)
            {
                sb.AppendLine($"{indent}printf(\"\\n\");");
            }
            else if (expr is CppStringLiteral strLit)
            {
                sb.AppendLine($"{indent}printf(\"{EscapeForPrintf(strLit.Value)}\");");
            }
            else if (expr is CppCharLiteral charLit)
            {
                sb.AppendLine($"{indent}printf(\"%c\", {FormatCharLiteral(charLit.Value)});");
            }
            else
            {
                var format = this.InferFormatSpecifier(expr);
                sb.AppendLine($"{indent}printf(\"{format}\", {this.ExprToString(expr)});");
            }
        }
    }

    private void EmitCinStatement(CppCinStatement stmt, StringBuilder sb, string indent)
    {
        foreach (var target in stmt.Targets)
        {
            var format = this.InferFormatSpecifier(target);
            var targetStr = this.ExprToString(target);

            // For scanf, we need &variable (unless it's already a pointer)
            if (target is CppIdentifierExpression || target is CppMemberAccessExpression)
            {
                sb.AppendLine($"{indent}scanf(\"{format}\", &{targetStr});");
            }
            else
            {
                sb.AppendLine($"{indent}scanf(\"{format}\", {targetStr});");
            }
        }
    }

    private string InferFormatSpecifier(CppExpressionNode expr)
    {
        // Try to infer from known variable types
        if (expr is CppIdentifierExpression id)
        {
            if (this.variableTypes.TryGetValue(id.Name, out var typeStr))
            {
                return TypeToFormatSpec(typeStr);
            }
        }

        // For integer literals, use %d
        if (expr is CppIntegerLiteral || expr is CppBoolLiteral)
        {
            return "%d";
        }

        if (expr is CppCharLiteral)
        {
            return "%c";
        }

        if (expr is CppStringLiteral)
        {
            return "%s";
        }

        // Default to %d for expressions
        return "%d";
    }

    // ────────────────────────────────────────────────────────
    //  Expressions → C strings
    // ────────────────────────────────────────────────────────
    private string ExprToString(CppExpressionNode expr)
    {
        return expr switch
        {
            CppIntegerLiteral intLit => intLit.Value.ToString(),
            CppCharLiteral charLit => FormatCharLiteral(charLit.Value),
            CppStringLiteral strLit => $"\"{EscapeForC(strLit.Value)}\"",
            CppBoolLiteral boolLit => boolLit.Value ? "1" : "0",
            CppNullptrLiteral => "0",
            CppThisExpression => "this_ptr",
            CppEndlExpression => "\"\\n\"",
            CppIdentifierExpression id => this.EmitIdentifier(id),
            CppBinaryExpression bin => this.EmitBinary(bin),
            CppUnaryExpression unary => this.EmitUnary(unary),
            CppPostfixExpression postfix => this.EmitPostfix(postfix),
            CppCallExpression call => this.EmitCall(call),
            CppMemberAccessExpression mem => this.EmitMemberAccess(mem),
            CppArrayAccessExpression arr => $"{this.ExprToString(arr.Array)}[{this.ExprToString(arr.Index)}]",
            CppNewExpression newExpr => this.EmitNew(newExpr),
            CppDeleteExpression delExpr => this.EmitDelete(delExpr),
            CppCastExpression cast => $"({this.TypeToString(cast.Type)}){this.ExprToString(cast.Operand)}",
            CppSizeofExpression sof => sof.Type is not null
                ? $"sizeof({this.TypeToString(sof.Type)})"
                : $"sizeof({this.ExprToString(sof.Operand!)})",
            CppCompoundAssignmentExpression compound => this.EmitCompoundAssign(compound),
            CppTernaryExpression tern => $"({this.ExprToString(tern.Condition)} ? {this.ExprToString(tern.TrueExpr)} : {this.ExprToString(tern.FalseExpr)})",
            CppScopeResolutionExpression scope => $"{scope.Scope}_{scope.Member}",
            _ => "/* unsupported expr */",
        };
    }

    private string EmitIdentifier(CppIdentifierExpression id)
    {
        // Reference parameters are automatically dereferenced in C
        if (this.referenceParameters.Contains(id.Name))
        {
            return $"(*{id.Name})";
        }

        // Inside a method body, bare identifiers that match a class field
        // (and are not shadowed by a local variable or parameter) must be
        // rewritten to this_ptr->field so the generated C code compiles.
        if (this.currentClassName is not null
            && !this.variableTypes.ContainsKey(id.Name)
            && this.classRegistry.TryGetValue(this.currentClassName, out var classInfo)
            && classInfo.Fields.Exists(f => f.Name == id.Name))
        {
            return $"this_ptr->{id.Name}";
        }

        return id.Name;
    }

    private string EmitBinary(CppBinaryExpression bin)
    {
        var left = this.ExprToString(bin.Left);
        var right = this.ExprToString(bin.Right);
        var op = bin.Operator switch
        {
            CppBinaryOperator.Add => "+",
            CppBinaryOperator.Sub => "-",
            CppBinaryOperator.Mul => "*",
            CppBinaryOperator.Div => "/",
            CppBinaryOperator.Mod => "%",
            CppBinaryOperator.And => "&&",
            CppBinaryOperator.Or => "||",
            CppBinaryOperator.BitwiseAnd => "&",
            CppBinaryOperator.BitwiseOr => "|",
            CppBinaryOperator.BitwiseXor => "^",
            CppBinaryOperator.Shl => "<<",
            CppBinaryOperator.Shr => ">>",
            CppBinaryOperator.Equal => "==",
            CppBinaryOperator.NotEqual => "!=",
            CppBinaryOperator.Less => "<",
            CppBinaryOperator.LessEqual => "<=",
            CppBinaryOperator.Greater => ">",
            CppBinaryOperator.GreaterEqual => ">=",
            CppBinaryOperator.Assign => "=",
            _ => "/* unknown op */",
        };

        // Handle assignment to reference parameter (dereference LHS)
        if (bin.Operator == CppBinaryOperator.Assign && bin.Left is CppIdentifierExpression assignId
            && this.referenceParameters.Contains(assignId.Name))
        {
            return $"(*{assignId.Name}) = {right}";
        }

        return $"({left} {op} {right})";
    }

    private string EmitUnary(CppUnaryExpression unary)
    {
        var operand = this.ExprToString(unary.Operand);
        return unary.Operator switch
        {
            CppUnaryOperator.Negate => $"(-{operand})",
            CppUnaryOperator.LogicalNot => $"(!{operand})",
            CppUnaryOperator.BitwiseNot => $"(~{operand})",
            CppUnaryOperator.Dereference => $"(*{operand})",
            CppUnaryOperator.AddressOf => $"(&{operand})",
            CppUnaryOperator.PreIncrement => $"(++{operand})",
            CppUnaryOperator.PreDecrement => $"(--{operand})",
            _ => operand,
        };
    }

    private string EmitPostfix(CppPostfixExpression postfix)
    {
        var operand = this.ExprToString(postfix.Operand);
        return postfix.Operator switch
        {
            CppPostfixOperator.Increment => $"({operand}++)",
            CppPostfixOperator.Decrement => $"({operand}--)",
            _ => operand,
        };
    }

    private string EmitCall(CppCallExpression call)
    {
        // Method call on object: obj.method(args) → ClassName_method(&obj, args)
        if (call.Callee is CppMemberAccessExpression mem)
        {
            var className = this.ResolveObjectType(mem.Object);
            var ownerClass = className is not null ? this.FindMethodOwnerClass(className, mem.Member) : null;
            if (ownerClass is not null)
            {
                var mangledName = CppNameMangler.MangleMethod(ownerClass, mem.Member);
                var objStr = mem.IsArrow
                    ? this.ExprToString(mem.Object)
                    : $"&{this.ExprToString(mem.Object)}";

                // Cast to base struct pointer when calling an inherited method
                if (ownerClass != className)
                {
                    objStr = $"(struct {ownerClass}*){objStr}";
                }

                var argStrs = this.EmitCallArguments(call.Arguments, mangledName);
                argStrs.Insert(0, objStr);
                return $"{mangledName}({string.Join(", ", argStrs)})";
            }
        }

        // Regular function call
        var callee = this.ExprToString(call.Callee);
        var funcName = call.Callee is CppIdentifierExpression id ? id.Name : null;
        var argStrs2 = this.EmitCallArguments(call.Arguments, funcName);
        return $"{callee}({string.Join(", ", argStrs2)})";
    }

    private List<string> EmitCallArguments(IReadOnlyList<CppExpressionNode> arguments, string? funcName)
    {
        List<bool>? refFlags = null;
        List<CppExpressionNode?>? defaults = null;
        if (funcName is not null)
        {
            this.functionRefParams.TryGetValue(funcName, out refFlags);
            this.functionDefaults.TryGetValue(funcName, out defaults);
        }

        // Determine total argument count including defaults for missing args
        var totalCount = arguments.Count;
        if (defaults is not null && arguments.Count < defaults.Count)
        {
            totalCount = defaults.Count;
        }

        var result = new List<string>(totalCount);
        for (var i = 0; i < totalCount; i++)
        {
            CppExpressionNode arg;
            if (i < arguments.Count)
            {
                arg = arguments[i];
            }
            else if (defaults is not null && i < defaults.Count && defaults[i] is not null)
            {
                arg = defaults[i]!;
            }
            else
            {
                break;
            }

            var argStr = this.ExprToString(arg);
            if (refFlags is not null && i < refFlags.Count && refFlags[i]
                && arg is CppIdentifierExpression or CppMemberAccessExpression or CppArrayAccessExpression
                && !this.referenceParameters.Contains((arg as CppIdentifierExpression)?.Name ?? string.Empty))
            {
                argStr = $"&{argStr}";
            }

            result.Add(argStr);
        }

        return result;
    }

    private string EmitMemberAccess(CppMemberAccessExpression mem)
    {
        var obj = this.ExprToString(mem.Object);
        var op = mem.IsArrow ? "->" : ".";

        // this->member or this.member → this_ptr->member
        if (mem.Object is CppThisExpression)
        {
            return $"this_ptr->{mem.Member}";
        }

        return $"{obj}{op}{mem.Member}";
    }

    private string EmitNew(CppNewExpression newExpr)
    {
        this.NeedsStdlib = true;
        var typeStr = this.TypeToString(newExpr.Type);

        // Check if it's a class type
        if (newExpr.Type is CppNamedType namedType && this.classRegistry.ContainsKey(namedType.Name))
        {
            // new ClassName(args) → { malloc + constructor call }
            // Since C doesn't support comma expressions well here, we'll use a simple pattern
            return $"(struct {namedType.Name}*)malloc(sizeof(struct {namedType.Name}))";
        }

        // Primitive type: new int → (int*)malloc(sizeof(int))
        return $"({typeStr}*)malloc(sizeof({typeStr}))";
    }

    private string EmitDelete(CppDeleteExpression delExpr)
    {
        this.NeedsStdlib = true;
        var operand = this.ExprToString(delExpr.Operand);

        // Check if it's a class pointer and call destructor
        // For simplicity, just emit free()
        return $"free({operand})";
    }

    private string EmitCompoundAssign(CppCompoundAssignmentExpression compound)
    {
        var left = this.ExprToString(compound.Left);
        var right = this.ExprToString(compound.Right);
        var op = compound.Operator switch
        {
            CppBinaryOperator.Add => "+=",
            CppBinaryOperator.Sub => "-=",
            CppBinaryOperator.Mul => "*=",
            CppBinaryOperator.Div => "/=",
            CppBinaryOperator.Mod => "%=",
            CppBinaryOperator.BitwiseAnd => "&=",
            CppBinaryOperator.BitwiseOr => "|=",
            CppBinaryOperator.BitwiseXor => "^=",
            CppBinaryOperator.Shl => "<<=",
            CppBinaryOperator.Shr => ">>=",
            _ => "/* unknown */",
        };

        return $"{left} {op} {right}";
    }

    // ────────────────────────────────────────────────────────
    //  Type helpers
    // ────────────────────────────────────────────────────────
    private string TypeToString(CppTypeNode type)
    {
        return type switch
        {
            CppPrimitiveType prim => PrimitiveToString(prim),
            CppPointerType ptr => this.TypeToString(ptr.Inner) + "*",
            CppReferenceType refType => this.TypeToString(refType.Inner),   // References are transparently pointers
            CppArrayType arr => arr.Size is not null
                ? $"{this.TypeToString(arr.ElementType)}[{this.ExprToString(arr.Size)}]"
                : $"{this.TypeToString(arr.ElementType)}[]",
            CppNamedType named => this.NamedTypeToString(named),
            _ => "int",
        };
    }

    private string NamedTypeToString(CppNamedType named)
    {
        // string → char*
        if (named.Name == "string")
        {
            return "char*";
        }

        // Known class → struct ClassName
        if (this.classRegistry.ContainsKey(named.Name))
        {
            return $"struct {named.Name}";
        }

        return named.Name;
    }

    private string? FindMethodOwnerClass(string className, string methodName)
    {
        var current = className;
        while (current is not null && this.classRegistry.TryGetValue(current, out var info))
        {
            if (info.Methods.Contains(methodName))
            {
                return current;
            }

            current = info.BaseClassName;
        }

        return null;
    }

    private string? ResolveObjectType(CppExpressionNode expr)
    {
        if (expr is CppThisExpression)
        {
            return this.currentClassName;
        }

        if (expr is CppIdentifierExpression id && this.variableTypes.TryGetValue(id.Name, out var typeStr))
        {
            // "struct ClassName" → "ClassName"
            if (typeStr.StartsWith("struct ", StringComparison.Ordinal))
            {
                return typeStr["struct ".Length..];
            }

            return typeStr;
        }

        return null;
    }

    // ────────────────────────────────────────────────────────
    //  Supporting types
    // ────────────────────────────────────────────────────────
    private sealed class CppClassInfo
    {
        public required string Name { get; init; }

        public string? BaseClassName { get; init; }

        public List<(string Name, string Type)> Fields { get; } = [];

        public List<string> Methods { get; } = [];
    }
}
