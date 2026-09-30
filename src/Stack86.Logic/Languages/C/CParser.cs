namespace Stack86.Logic.Languages.C;

using Stack86.Common.Exceptions;

using Stack86.Logic.Pipeline.Ast;

/// <summary>
/// Recursive-descent parser for a subset of C.
/// Produces an AST from a token stream.
/// </summary>
public sealed class CParser(List<CToken> tokens)
{
    private readonly HashSet<string> typedefNames = [];
    private int position;

    /// <summary>
    /// Parses the token stream into a <see cref="ProgramNode"/>.
    /// </summary>
    /// <returns>The parsed program AST.</returns>
    public ProgramNode Parse()
    {
        var declarations = new List<DeclarationNode>();

        while (!this.IsAtEnd())
        {
            declarations.Add(this.ParseTopLevelDeclaration());
        }

        return new ProgramNode { Declarations = declarations };
    }

    private static bool IsTypeKind(CTokenKind kind)
    {
        return kind is CTokenKind.IntKeyword or CTokenKind.CharKeyword or CTokenKind.VoidKeyword
            or CTokenKind.StructKeyword or CTokenKind.EnumKeyword or CTokenKind.UnionKeyword;
    }

    // ────────────────────────────────────────────────────────
    //  Top-level
    // ────────────────────────────────────────────────────────
    private DeclarationNode ParseTopLevelDeclaration()
    {
        // Struct declaration: struct Name { ... };
        if (this.Check(CTokenKind.StructKeyword))
        {
            if (this.IsStructOrUnionDeclaration(CTokenKind.StructKeyword))
            {
                return this.ParseStructDeclaration();
            }
        }

        // Union declaration: union Name { ... };
        if (this.Check(CTokenKind.UnionKeyword))
        {
            if (this.IsStructOrUnionDeclaration(CTokenKind.UnionKeyword))
            {
                return this.ParseUnionDeclaration();
            }
        }

        // Enum declaration: enum Name { ... };
        if (this.Check(CTokenKind.EnumKeyword))
        {
            if (this.IsEnumDeclaration())
            {
                return this.ParseEnumDeclaration();
            }
        }

        // Typedef: typedef <type> <alias>;
        if (this.Check(CTokenKind.TypedefKeyword))
        {
            return this.ParseTypedefDeclaration();
        }

        var type = this.ParseType();

        // Forward declaration: struct/union/enum Name;
        if (this.Check(CTokenKind.Semicolon) && type is StructType or UnionType or EnumType)
        {
            this.Advance();
            return type switch
            {
                StructType st => new StructDeclaration { Name = st.Name, Fields = [], Line = st.Line, Column = st.Column },
                UnionType ut => new UnionDeclaration { Name = ut.Name, Fields = [], Line = ut.Line, Column = ut.Column },
                EnumType et => new EnumDeclaration { Name = et.Name, Members = [], Line = et.Line, Column = et.Column },
                _ => throw new CompilerInvariantException("Unreachable"),
            };
        }

        var name = this.Expect(CTokenKind.Identifier);

        // Function declaration
        if (this.Check(CTokenKind.LeftParen))
        {
            return this.ParseFunctionDeclaration(type, name);
        }

        // Array type suffix: int a[10];
        type = this.ParseArraySuffix(type);

        // Global variable
        ExpressionNode? init = null;
        if (this.Match(CTokenKind.Assign))
        {
            init = this.ParseExpression();
        }

        this.Expect(CTokenKind.Semicolon);

        return new VariableDeclaration
        {
            Type = type,
            Name = name.Text,
            Initializer = init,
            Line = name.Line,
            Column = name.Column,
        };
    }

    private FunctionDeclaration ParseFunctionDeclaration(TypeNode returnType, CToken name)
    {
        this.Expect(CTokenKind.LeftParen);
        var parameters = this.ParseParameterList();
        this.Expect(CTokenKind.RightParen);

        // Forward declaration (prototype): int foo(int x);
        if (this.Match(CTokenKind.Semicolon))
        {
            return new FunctionDeclaration
            {
                ReturnType = returnType,
                Name = name.Text,
                Parameters = parameters,
                Body = null,
                Line = name.Line,
                Column = name.Column,
            };
        }

        var body = this.ParseBlock();

        return new FunctionDeclaration
        {
            ReturnType = returnType,
            Name = name.Text,
            Parameters = parameters,
            Body = body,
            Line = name.Line,
            Column = name.Column,
        };
    }

    private List<ParameterDeclaration> ParseParameterList()
    {
        var parameters = new List<ParameterDeclaration>();

        if (this.Check(CTokenKind.RightParen))
        {
            return parameters;
        }

        // Handle (void)
        if (this.Check(CTokenKind.VoidKeyword) &&
            this.position + 1 < tokens.Count &&
            tokens[this.position + 1].Kind == CTokenKind.RightParen)
        {
            this.Advance();
            return parameters;
        }

        do
        {
            var type = this.ParseType();

            // The parameter name is optional in a prototype (e.g. `int foo(int, char);`).
            // When the declarator is absent, the next token is a comma or the closing paren.
            if (this.Check(CTokenKind.Identifier))
            {
                var name = this.Expect(CTokenKind.Identifier);
                type = this.ParseArraySuffix(type);
                parameters.Add(new ParameterDeclaration
                {
                    Type = type,
                    Name = name.Text,
                    Line = name.Line,
                    Column = name.Column,
                });
            }
            else
            {
                type = this.ParseArraySuffix(type);
                parameters.Add(new ParameterDeclaration
                {
                    Type = type,
                    Name = string.Empty,
                    Line = this.Current().Line,
                    Column = this.Current().Column,
                });
            }
        }
        while (this.Match(CTokenKind.Comma));

        return parameters;
    }

    // ────────────────────────────────────────────────────────
    //  Structs, Unions, Enums, Typedefs
    // ────────────────────────────────────────────────────────

    /// <summary>
    /// Checks whether the current position is a struct/union type declaration
    /// (i.e. <c>struct Name { ... };</c>) as opposed to a struct-typed variable.
    /// </summary>
    private bool IsStructOrUnionDeclaration(CTokenKind keyword)
    {
        if (!this.Check(keyword))
        {
            return false;
        }

        var lookahead = this.position + 1;
        if (lookahead >= tokens.Count || tokens[lookahead].Kind != CTokenKind.Identifier)
        {
            return false;
        }

        lookahead++;
        return lookahead < tokens.Count && tokens[lookahead].Kind == CTokenKind.LeftBrace;
    }

    /// <summary>
    /// Checks whether the current position is an enum declaration
    /// (i.e. <c>enum Name { ... };</c>) as opposed to an enum-typed variable.
    /// </summary>
    private bool IsEnumDeclaration()
    {
        if (!this.Check(CTokenKind.EnumKeyword))
        {
            return false;
        }

        var lookahead = this.position + 1;
        if (lookahead >= tokens.Count || tokens[lookahead].Kind != CTokenKind.Identifier)
        {
            return false;
        }

        lookahead++;
        return lookahead < tokens.Count && tokens[lookahead].Kind == CTokenKind.LeftBrace;
    }

    private StructDeclaration ParseStructDeclaration()
    {
        var keyword = this.Expect(CTokenKind.StructKeyword);
        var name = this.Expect(CTokenKind.Identifier);
        this.Expect(CTokenKind.LeftBrace);

        var fields = new List<StructFieldDeclaration>();

        while (!this.Check(CTokenKind.RightBrace) && !this.IsAtEnd())
        {
            var fieldType = this.ParseType();
            var fieldName = this.Expect(CTokenKind.Identifier);
            this.Expect(CTokenKind.Semicolon);

            fields.Add(new StructFieldDeclaration
            {
                Type = fieldType,
                Name = fieldName.Text,
                Line = fieldName.Line,
                Column = fieldName.Column,
            });
        }

        this.Expect(CTokenKind.RightBrace);
        this.Expect(CTokenKind.Semicolon);

        return new StructDeclaration
        {
            Name = name.Text,
            Fields = fields,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private UnionDeclaration ParseUnionDeclaration()
    {
        var keyword = this.Expect(CTokenKind.UnionKeyword);
        var name = this.Expect(CTokenKind.Identifier);
        this.Expect(CTokenKind.LeftBrace);

        var fields = new List<StructFieldDeclaration>();

        while (!this.Check(CTokenKind.RightBrace) && !this.IsAtEnd())
        {
            var fieldType = this.ParseType();
            var fieldName = this.Expect(CTokenKind.Identifier);
            this.Expect(CTokenKind.Semicolon);

            fields.Add(new StructFieldDeclaration
            {
                Type = fieldType,
                Name = fieldName.Text,
                Line = fieldName.Line,
                Column = fieldName.Column,
            });
        }

        this.Expect(CTokenKind.RightBrace);
        this.Expect(CTokenKind.Semicolon);

        return new UnionDeclaration
        {
            Name = name.Text,
            Fields = fields,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private EnumDeclaration ParseEnumDeclaration()
    {
        var keyword = this.Expect(CTokenKind.EnumKeyword);
        var name = this.Expect(CTokenKind.Identifier);
        this.Expect(CTokenKind.LeftBrace);

        var members = new List<EnumMemberNode>();

        while (!this.Check(CTokenKind.RightBrace) && !this.IsAtEnd())
        {
            var memberName = this.Expect(CTokenKind.Identifier);
            ExpressionNode? memberValue = null;

            if (this.Match(CTokenKind.Assign))
            {
                memberValue = this.ParseExpression();
            }

            members.Add(new EnumMemberNode
            {
                Name = memberName.Text,
                Value = memberValue,
                Line = memberName.Line,
                Column = memberName.Column,
            });

            if (!this.Check(CTokenKind.RightBrace))
            {
                this.Expect(CTokenKind.Comma);
            }
        }

        this.Expect(CTokenKind.RightBrace);
        this.Expect(CTokenKind.Semicolon);

        return new EnumDeclaration
        {
            Name = name.Text,
            Members = members,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private TypedefDeclaration ParseTypedefDeclaration()
    {
        var keyword = this.Expect(CTokenKind.TypedefKeyword);
        var originalType = this.ParseType();
        var aliasName = this.Expect(CTokenKind.Identifier);
        this.Expect(CTokenKind.Semicolon);

        this.typedefNames.Add(aliasName.Text);

        return new TypedefDeclaration
        {
            OriginalType = originalType,
            AliasName = aliasName.Text,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Types
    // ────────────────────────────────────────────────────────
    private TypeNode ParseType()
    {
        var token = this.Current();
        TypeNode baseType;

        if (this.Match(CTokenKind.IntKeyword))
        {
            baseType = new PrimitiveType { Kind = PrimitiveKind.Int, Line = token.Line, Column = token.Column };
        }
        else if (this.Match(CTokenKind.CharKeyword))
        {
            baseType = new PrimitiveType { Kind = PrimitiveKind.Char, Line = token.Line, Column = token.Column };
        }
        else if (this.Match(CTokenKind.VoidKeyword))
        {
            baseType = new PrimitiveType { Kind = PrimitiveKind.Void, Line = token.Line, Column = token.Column };
        }
        else if (this.Match(CTokenKind.StructKeyword))
        {
            var name = this.Expect(CTokenKind.Identifier);
            baseType = new StructType { Name = name.Text, Line = token.Line, Column = token.Column };
        }
        else if (this.Match(CTokenKind.EnumKeyword))
        {
            var name = this.Expect(CTokenKind.Identifier);
            baseType = new EnumType { Name = name.Text, Line = token.Line, Column = token.Column };
        }
        else if (this.Match(CTokenKind.UnionKeyword))
        {
            var name = this.Expect(CTokenKind.Identifier);
            baseType = new UnionType { Name = name.Text, Line = token.Line, Column = token.Column };
        }
        else if (token.Kind == CTokenKind.Identifier && this.typedefNames.Contains(token.Text))
        {
            this.Advance();
            baseType = new TypedefNameType { Name = token.Text, Line = token.Line, Column = token.Column };
        }
        else
        {
            throw new CompilerInvariantException(
                $"Expected type at line {token.Line}, column {token.Column}, got '{token.Text}'.");
        }

        // Pointer types
        while (this.Match(CTokenKind.Asterisk))
        {
            baseType = new PointerType { Inner = baseType, Line = baseType.Line, Column = baseType.Column };
        }

        return baseType;
    }

    // ────────────────────────────────────────────────────────
    //  Statements
    // ────────────────────────────────────────────────────────
    private BlockStatement ParseBlock()
    {
        var open = this.Expect(CTokenKind.LeftBrace);
        var statements = new List<StatementNode>();

        while (!this.Check(CTokenKind.RightBrace) && !this.IsAtEnd())
        {
            statements.Add(this.ParseStatement());
        }

        this.Expect(CTokenKind.RightBrace);

        return new BlockStatement
        {
            Statements = statements,
            Line = open.Line,
            Column = open.Column,
        };
    }

    private StatementNode ParseStatement()
    {
        if (this.Check(CTokenKind.LeftBrace))
        {
            return this.ParseBlock();
        }

        if (this.Check(CTokenKind.ReturnKeyword))
        {
            return this.ParseReturn();
        }

        if (this.Check(CTokenKind.IfKeyword))
        {
            return this.ParseIf();
        }

        if (this.Check(CTokenKind.WhileKeyword))
        {
            return this.ParseWhile();
        }

        if (this.Check(CTokenKind.ForKeyword))
        {
            return this.ParseFor();
        }

        if (this.Check(CTokenKind.DoKeyword))
        {
            return this.ParseDoWhile();
        }

        if (this.Check(CTokenKind.SwitchKeyword))
        {
            return this.ParseSwitch();
        }

        if (this.Check(CTokenKind.BreakKeyword))
        {
            var t = this.Advance();
            this.Expect(CTokenKind.Semicolon);
            return new BreakStatement { Line = t.Line, Column = t.Column };
        }

        if (this.Check(CTokenKind.ContinueKeyword))
        {
            var t = this.Advance();
            this.Expect(CTokenKind.Semicolon);
            return new ContinueStatement { Line = t.Line, Column = t.Column };
        }

        // Local variable declaration (starts with a type keyword, including struct/enum/union)
        if (this.IsTypeKeyword())
        {
            return this.ParseLocalVariable();
        }

        // Expression statement
        var expr = this.ParseExpression();
        this.Expect(CTokenKind.Semicolon);
        return new ExpressionStatement { Expression = expr, Line = expr.Line, Column = expr.Column };
    }

    private ReturnStatement ParseReturn()
    {
        var token = this.Advance(); // consume 'return'
        ExpressionNode? expr = null;

        if (!this.Check(CTokenKind.Semicolon))
        {
            expr = this.ParseExpression();
        }

        this.Expect(CTokenKind.Semicolon);
        return new ReturnStatement { Expression = expr, Line = token.Line, Column = token.Column };
    }

    private IfStatement ParseIf()
    {
        var token = this.Advance(); // consume 'if'
        this.Expect(CTokenKind.LeftParen);
        var condition = this.ParseExpression();
        this.Expect(CTokenKind.RightParen);
        var then = this.ParseStatement();

        StatementNode? elseStmt = null;
        if (this.Match(CTokenKind.ElseKeyword))
        {
            elseStmt = this.ParseStatement();
        }

        return new IfStatement
        {
            Condition = condition,
            Then = then,
            Else = elseStmt,
            Line = token.Line,
            Column = token.Column,
        };
    }

    private WhileStatement ParseWhile()
    {
        var token = this.Advance(); // consume 'while'
        this.Expect(CTokenKind.LeftParen);
        var condition = this.ParseExpression();
        this.Expect(CTokenKind.RightParen);
        var body = this.ParseStatement();

        return new WhileStatement
        {
            Condition = condition,
            Body = body,
            Line = token.Line,
            Column = token.Column,
        };
    }

    private ForStatement ParseFor()
    {
        var token = this.Advance(); // consume 'for'
        this.Expect(CTokenKind.LeftParen);

        StatementNode? init = null;
        if (!this.Check(CTokenKind.Semicolon))
        {
            if (this.IsTypeKeyword())
            {
                init = this.ParseLocalVariable();
            }
            else
            {
                var expr = this.ParseExpression();
                this.Expect(CTokenKind.Semicolon);
                init = new ExpressionStatement { Expression = expr, Line = expr.Line, Column = expr.Column };
            }
        }
        else
        {
            this.Advance(); // consume ';'
        }

        ExpressionNode? condition = null;
        if (!this.Check(CTokenKind.Semicolon))
        {
            condition = this.ParseExpression();
        }

        this.Expect(CTokenKind.Semicolon);

        ExpressionNode? increment = null;
        if (!this.Check(CTokenKind.RightParen))
        {
            increment = this.ParseExpression();
        }

        this.Expect(CTokenKind.RightParen);
        var body = this.ParseStatement();

        return new ForStatement
        {
            Init = init,
            Condition = condition,
            Increment = increment,
            Body = body,
            Line = token.Line,
            Column = token.Column,
        };
    }

    private VariableDeclarationStatement ParseLocalVariable()
    {
        var type = this.ParseType();

        string name;
        int nameLine;
        int nameColumn;

        // Function-pointer declarator: returnType (*name)(paramTypes)
        if (this.Check(CTokenKind.LeftParen) &&
            this.position + 1 < tokens.Count &&
            tokens[this.position + 1].Kind == CTokenKind.Asterisk)
        {
            this.Expect(CTokenKind.LeftParen);
            this.Expect(CTokenKind.Asterisk);
            var ptrName = this.Expect(CTokenKind.Identifier);
            name = ptrName.Text;
            nameLine = ptrName.Line;
            nameColumn = ptrName.Column;
            this.Expect(CTokenKind.RightParen);
            this.Expect(CTokenKind.LeftParen);
            var parameters = this.ParseParameterList();
            this.Expect(CTokenKind.RightParen);
            type = new FunctionPointerType
            {
                ReturnType = type,
                Parameters = [.. parameters.Select(p => p.Type)],
                Line = type.Line,
                Column = type.Column,
            };
        }
        else
        {
            var id = this.Expect(CTokenKind.Identifier);
            name = id.Text;
            nameLine = id.Line;
            nameColumn = id.Column;

            // Array type suffix: int a[10];
            type = this.ParseArraySuffix(type);
        }

        ExpressionNode? init = null;
        if (this.Match(CTokenKind.Assign))
        {
            init = this.ParseExpression();
        }

        this.Expect(CTokenKind.Semicolon);

        return new VariableDeclarationStatement
        {
            Declaration = new VariableDeclaration
            {
                Type = type,
                Name = name,
                Initializer = init,
                Line = nameLine,
                Column = nameColumn,
            },
            Line = type.Line,
            Column = type.Column,
        };
    }

    private DoWhileStatement ParseDoWhile()
    {
        var token = this.Advance(); // consume 'do'
        var body = this.ParseStatement();
        this.Expect(CTokenKind.WhileKeyword);
        this.Expect(CTokenKind.LeftParen);
        var condition = this.ParseExpression();
        this.Expect(CTokenKind.RightParen);
        this.Expect(CTokenKind.Semicolon);

        return new DoWhileStatement
        {
            Body = body,
            Condition = condition,
            Line = token.Line,
            Column = token.Column,
        };
    }

    private SwitchStatement ParseSwitch()
    {
        var token = this.Advance(); // consume 'switch'
        this.Expect(CTokenKind.LeftParen);
        var expression = this.ParseExpression();
        this.Expect(CTokenKind.RightParen);
        this.Expect(CTokenKind.LeftBrace);

        var cases = new List<CaseClause>();

        while (!this.Check(CTokenKind.RightBrace) && !this.IsAtEnd())
        {
            ExpressionNode? caseValue = null;

            if (this.Match(CTokenKind.CaseKeyword))
            {
                caseValue = this.ParseExpression();
                this.Expect(CTokenKind.Colon);
            }
            else if (this.Match(CTokenKind.DefaultKeyword))
            {
                this.Expect(CTokenKind.Colon);
            }
            else
            {
                break;
            }

            var body = new List<StatementNode>();
            while (!this.Check(CTokenKind.CaseKeyword) &&
                   !this.Check(CTokenKind.DefaultKeyword) &&
                   !this.Check(CTokenKind.RightBrace) &&
                   !this.IsAtEnd())
            {
                body.Add(this.ParseStatement());
            }

            cases.Add(new CaseClause
            {
                Value = caseValue,
                Body = body,
                Line = token.Line,
                Column = token.Column,
            });
        }

        this.Expect(CTokenKind.RightBrace);

        return new SwitchStatement
        {
            Expression = expression,
            Cases = cases,
            Line = token.Line,
            Column = token.Column,
        };
    }

    /// <summary>
    /// Parses optional array size suffixes: <c>[10]</c>, <c>[3][4]</c>.
    /// </summary>
    private TypeNode ParseArraySuffix(TypeNode baseType)
    {
        while (this.Match(CTokenKind.LeftBracket))
        {
            var sizeToken = this.Expect(CTokenKind.IntegerLiteral);
            this.Expect(CTokenKind.RightBracket);
            baseType = new ArrayType
            {
                ElementType = baseType,
                Size = sizeToken.IntValue,
                Line = baseType.Line,
                Column = baseType.Column,
            };
        }

        return baseType;
    }

    // ────────────────────────────────────────────────────────
    //  Expressions (precedence climbing)
    // ────────────────────────────────────────────────────────
    private ExpressionNode ParseExpression()
    {
        return this.ParseComma();
    }

    private ExpressionNode ParseComma()
    {
        var left = this.ParseAssignment();

        while (this.Match(CTokenKind.Comma))
        {
            var right = this.ParseAssignment();
            left = new CommaExpression
            {
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseAssignment()
    {
        var left = this.ParseTernary();

        if (this.Match(CTokenKind.Assign))
        {
            var right = this.ParseAssignment(); // right-associative
            return new BinaryExpression
            {
                Operator = BinaryOperator.Assign,
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        // Compound assignment operators
        var compoundOp = this.Current().Kind switch
        {
            CTokenKind.PlusEqual => (BinaryOperator?)BinaryOperator.Add,
            CTokenKind.MinusEqual => (BinaryOperator?)BinaryOperator.Sub,
            CTokenKind.StarEqual => (BinaryOperator?)BinaryOperator.Mul,
            CTokenKind.SlashEqual => (BinaryOperator?)BinaryOperator.Div,
            CTokenKind.PercentEqual => (BinaryOperator?)BinaryOperator.Mod,
            CTokenKind.AmpEqual => (BinaryOperator?)BinaryOperator.BitwiseAnd,
            CTokenKind.PipeEqual => (BinaryOperator?)BinaryOperator.BitwiseOr,
            CTokenKind.CaretEqual => (BinaryOperator?)BinaryOperator.BitwiseXor,
            CTokenKind.ShiftLeftEqual => (BinaryOperator?)BinaryOperator.Shl,
            CTokenKind.ShiftRightEqual => (BinaryOperator?)BinaryOperator.Shr,
            _ => null,
        };

        if (compoundOp.HasValue)
        {
            this.Advance();
            var right = this.ParseAssignment();
            return new CompoundAssignmentExpression
            {
                Operator = compoundOp.Value,
                Target = left,
                Value = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseTernary()
    {
        var condition = this.ParseLogicalOr();

        if (this.Match(CTokenKind.Question))
        {
            var thenExpr = this.ParseExpression();
            this.Expect(CTokenKind.Colon);
            var elseExpr = this.ParseTernary();
            return new TernaryExpression
            {
                Condition = condition,
                ThenExpression = thenExpr,
                ElseExpression = elseExpr,
                Line = condition.Line,
                Column = condition.Column,
            };
        }

        return condition;
    }

    private ExpressionNode ParseLogicalOr()
    {
        var left = this.ParseLogicalAnd();

        while (this.Match(CTokenKind.LogicalOr))
        {
            var right = this.ParseLogicalAnd();
            left = new BinaryExpression
            {
                Operator = BinaryOperator.Or,
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseLogicalAnd()
    {
        var left = this.ParseBitwiseOr();

        while (this.Match(CTokenKind.LogicalAnd))
        {
            var right = this.ParseBitwiseOr();
            left = new BinaryExpression
            {
                Operator = BinaryOperator.And,
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseBitwiseOr()
    {
        var left = this.ParseBitwiseXor();

        while (this.Match(CTokenKind.Pipe))
        {
            var right = this.ParseBitwiseXor();
            left = new BinaryExpression
            {
                Operator = BinaryOperator.BitwiseOr,
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseBitwiseXor()
    {
        var left = this.ParseBitwiseAnd();

        while (this.Match(CTokenKind.Caret))
        {
            var right = this.ParseBitwiseAnd();
            left = new BinaryExpression
            {
                Operator = BinaryOperator.BitwiseXor,
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseBitwiseAnd()
    {
        var left = this.ParseEquality();

        while (this.Check(CTokenKind.Ampersand) && !this.CheckNext(CTokenKind.Ampersand))
        {
            this.Advance();
            var right = this.ParseEquality();
            left = new BinaryExpression
            {
                Operator = BinaryOperator.BitwiseAnd,
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseEquality()
    {
        var left = this.ParseComparison();

        while (true)
        {
            BinaryOperator? op = null;

            if (this.Match(CTokenKind.Equal))
            {
                op = BinaryOperator.Equal;
            }
            else if (this.Match(CTokenKind.NotEqual))
            {
                op = BinaryOperator.NotEqual;
            }

            if (op == null)
            {
                break;
            }

            var right = this.ParseComparison();
            left = new BinaryExpression
            {
                Operator = op.Value,
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseComparison()
    {
        var left = this.ParseShift();

        while (true)
        {
            BinaryOperator? op = null;

            if (this.Match(CTokenKind.Less))
            {
                op = BinaryOperator.Less;
            }
            else if (this.Match(CTokenKind.LessEqual))
            {
                op = BinaryOperator.LessEqual;
            }
            else if (this.Match(CTokenKind.Greater))
            {
                op = BinaryOperator.Greater;
            }
            else if (this.Match(CTokenKind.GreaterEqual))
            {
                op = BinaryOperator.GreaterEqual;
            }

            if (op == null)
            {
                break;
            }

            var right = this.ParseShift();
            left = new BinaryExpression
            {
                Operator = op.Value,
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseShift()
    {
        var left = this.ParseAdditive();

        while (true)
        {
            BinaryOperator? op = null;

            if (this.Match(CTokenKind.ShiftLeft))
            {
                op = BinaryOperator.Shl;
            }
            else if (this.Match(CTokenKind.ShiftRight))
            {
                op = BinaryOperator.Shr;
            }

            if (op == null)
            {
                break;
            }

            var right = this.ParseAdditive();
            left = new BinaryExpression
            {
                Operator = op.Value,
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseAdditive()
    {
        var left = this.ParseMultiplicative();

        while (true)
        {
            BinaryOperator? op = null;

            if (this.Match(CTokenKind.Plus))
            {
                op = BinaryOperator.Add;
            }
            else if (this.Match(CTokenKind.Minus))
            {
                op = BinaryOperator.Sub;
            }

            if (op == null)
            {
                break;
            }

            var right = this.ParseMultiplicative();
            left = new BinaryExpression
            {
                Operator = op.Value,
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseMultiplicative()
    {
        var left = this.ParseUnary();

        while (true)
        {
            BinaryOperator? op = null;

            if (this.Match(CTokenKind.Asterisk))
            {
                op = BinaryOperator.Mul;
            }
            else if (this.Match(CTokenKind.Slash))
            {
                op = BinaryOperator.Div;
            }
            else if (this.Match(CTokenKind.Percent))
            {
                op = BinaryOperator.Mod;
            }

            if (op == null)
            {
                break;
            }

            var right = this.ParseUnary();
            left = new BinaryExpression
            {
                Operator = op.Value,
                Left = left,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private ExpressionNode ParseUnary()
    {
        if (this.Match(CTokenKind.Minus))
        {
            var operand = this.ParseUnary();
            return new UnaryExpression
            {
                Operator = UnaryOperator.Negate,
                Operand = operand,
                Line = operand.Line,
                Column = operand.Column,
            };
        }

        if (this.Match(CTokenKind.Exclamation))
        {
            var operand = this.ParseUnary();
            return new UnaryExpression
            {
                Operator = UnaryOperator.LogicalNot,
                Operand = operand,
                Line = operand.Line,
                Column = operand.Column,
            };
        }

        if (this.Match(CTokenKind.Tilde))
        {
            var operand = this.ParseUnary();
            return new UnaryExpression
            {
                Operator = UnaryOperator.BitwiseNot,
                Operand = operand,
                Line = operand.Line,
                Column = operand.Column,
            };
        }

        if (this.Match(CTokenKind.PlusPlus))
        {
            var operand = this.ParseUnary();
            return new UnaryExpression
            {
                Operator = UnaryOperator.PreIncrement,
                Operand = operand,
                Line = operand.Line,
                Column = operand.Column,
            };
        }

        if (this.Match(CTokenKind.MinusMinus))
        {
            var operand = this.ParseUnary();
            return new UnaryExpression
            {
                Operator = UnaryOperator.PreDecrement,
                Operand = operand,
                Line = operand.Line,
                Column = operand.Column,
            };
        }

        if (this.Check(CTokenKind.SizeofKeyword))
        {
            return this.ParseSizeof();
        }

        if (this.Check(CTokenKind.Asterisk))
        {
            this.Advance();
            var operand = this.ParseUnary();
            return new UnaryExpression
            {
                Operator = UnaryOperator.Dereference,
                Operand = operand,
                Line = operand.Line,
                Column = operand.Column,
            };
        }

        if (this.Check(CTokenKind.Ampersand) && !this.CheckNext(CTokenKind.Ampersand))
        {
            this.Advance();
            var operand = this.ParseUnary();
            return new UnaryExpression
            {
                Operator = UnaryOperator.AddressOf,
                Operand = operand,
                Line = operand.Line,
                Column = operand.Column,
            };
        }

        return this.ParsePostfix();
    }

    private ExpressionNode ParsePostfix()
    {
        var expr = this.ParsePrimary();

        while (true)
        {
            if (this.Match(CTokenKind.LeftBracket))
            {
                var index = this.ParseExpression();
                this.Expect(CTokenKind.RightBracket);
                expr = new ArrayAccessExpression
                {
                    Array = expr,
                    Index = index,
                    Line = expr.Line,
                    Column = expr.Column,
                };
            }
            else if (this.Match(CTokenKind.Dot))
            {
                var member = this.Expect(CTokenKind.Identifier);
                expr = new MemberAccessExpression
                {
                    Object = expr,
                    MemberName = member.Text,
                    IsArrow = false,
                    Line = expr.Line,
                    Column = expr.Column,
                };
            }
            else if (this.Match(CTokenKind.Arrow))
            {
                var member = this.Expect(CTokenKind.Identifier);
                expr = new MemberAccessExpression
                {
                    Object = expr,
                    MemberName = member.Text,
                    IsArrow = true,
                    Line = expr.Line,
                    Column = expr.Column,
                };
            }
            else if (this.Match(CTokenKind.PlusPlus))
            {
                expr = new PostfixExpression
                {
                    Operator = PostfixOperator.Increment,
                    Operand = expr,
                    Line = expr.Line,
                    Column = expr.Column,
                };
            }
            else if (this.Match(CTokenKind.MinusMinus))
            {
                expr = new PostfixExpression
                {
                    Operator = PostfixOperator.Decrement,
                    Operand = expr,
                    Line = expr.Line,
                    Column = expr.Column,
                };
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private ExpressionNode ParsePrimary()
    {
        // Integer literal
        if (this.Check(CTokenKind.IntegerLiteral))
        {
            var token = this.Advance();
            return new IntegerLiteral { Value = token.IntValue, Line = token.Line, Column = token.Column };
        }

        // Char literal
        if (this.Check(CTokenKind.CharLiteral))
        {
            var token = this.Advance();
            return new CharLiteral { Value = token.CharValue, Line = token.Line, Column = token.Column };
        }

        // String literal
        if (this.Check(CTokenKind.StringLiteral))
        {
            var token = this.Advance();
            return new StringLiteral { Value = token.StringValue!, Line = token.Line, Column = token.Column };
        }

        // Parenthesised expression or cast
        if (this.Check(CTokenKind.LeftParen))
        {
            // Look ahead to distinguish cast from grouped expression
            if (this.IsTypeCast())
            {
                return this.ParseCast();
            }

            this.Advance(); // consume '('
            var expr = this.ParseExpression();
            this.Expect(CTokenKind.RightParen);
            return expr;
        }

        // Identifier (variable or function call)
        if (this.Check(CTokenKind.Identifier))
        {
            var token = this.Advance();

            // Function call
            if (this.Match(CTokenKind.LeftParen))
            {
                var args = new List<ExpressionNode>();

                if (!this.Check(CTokenKind.RightParen))
                {
                    do
                    {
                        args.Add(this.ParseAssignment());
                    }
                    while (this.Match(CTokenKind.Comma));
                }

                this.Expect(CTokenKind.RightParen);
                return new CallExpression
                {
                    FunctionName = token.Text,
                    Arguments = args,
                    Line = token.Line,
                    Column = token.Column,
                };
            }

            return new IdentifierExpression { Name = token.Text, Line = token.Line, Column = token.Column };
        }

        var current = this.Current();
        throw new CompilerInvariantException(
            $"Unexpected token '{current.Text}' at line {current.Line}, column {current.Column}.");
    }

    private CastExpression ParseCast()
    {
        var open = this.Advance(); // consume '('
        var targetType = this.ParseType();
        this.Expect(CTokenKind.RightParen);
        var operand = this.ParseUnary();

        return new CastExpression
        {
            TargetType = targetType,
            Operand = operand,
            Line = open.Line,
            Column = open.Column,
        };
    }

    private SizeofExpression ParseSizeof()
    {
        var token = this.Advance(); // consume 'sizeof'

        if (this.Check(CTokenKind.LeftParen))
        {
            // Could be sizeof(type) or sizeof(expr)
            var next = this.position + 1;
            if (next < tokens.Count && IsTypeKind(tokens[next].Kind))
            {
                this.Advance(); // consume '('
                var type = this.ParseType();
                this.Expect(CTokenKind.RightParen);
                return new SizeofExpression
                {
                    TargetType = type,
                    Line = token.Line,
                    Column = token.Column,
                };
            }
        }

        var operand = this.ParseUnary();
        return new SizeofExpression
        {
            Operand = operand,
            Line = token.Line,
            Column = token.Column,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────
    private bool IsTypeKeyword()
    {
        var token = this.Current();
        if (IsTypeKind(token.Kind))
        {
            return true;
        }

        return token.Kind == CTokenKind.Identifier && this.typedefNames.Contains(token.Text);
    }

    private bool IsTypeCast()
    {
        // (type) — look for '(' followed by a type keyword, then ')' or '*'
        if (!this.Check(CTokenKind.LeftParen))
        {
            return false;
        }

        var next = this.position + 1;
        if (next >= tokens.Count)
        {
            return false;
        }

        if (IsTypeKind(tokens[next].Kind))
        {
            return true;
        }

        return tokens[next].Kind == CTokenKind.Identifier && this.typedefNames.Contains(tokens[next].Text);
    }

    private CToken Current() => tokens[this.position];

    private bool IsAtEnd() => this.Current().Kind == CTokenKind.EndOfFile;

    private bool Check(CTokenKind kind) => !this.IsAtEnd() && this.Current().Kind == kind;

    private bool CheckNext(CTokenKind kind) =>
        this.position + 1 < tokens.Count && tokens[this.position + 1].Kind == kind;

    private bool Match(CTokenKind kind)
    {
        if (!this.Check(kind))
        {
            return false;
        }

        this.Advance();
        return true;
    }

    private CToken Advance()
    {
        var token = this.Current();
        this.position++;
        return token;
    }

    private CToken Expect(CTokenKind kind)
    {
        if (!this.Check(kind))
        {
            var current = this.Current();
            throw new CompilerInvariantException(
                $"Expected {kind} but got '{current.Text}' at line {current.Line}, column {current.Column}.");
        }

        return this.Advance();
    }
}
