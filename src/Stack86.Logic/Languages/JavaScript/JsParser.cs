namespace Stack86.Logic.Languages.JavaScript;

using Stack86.Logic.Languages.JavaScript.Ast;
using Stack86.Logic.Pipeline.Ir;

/// <summary>
/// Recursive-descent parser for JavaScript source code.
/// Produces a <see cref="JsProgramNode"/> AST. Reports diagnostics for syntax errors
/// rather than throwing, enabling partial compilation with error recovery.
/// </summary>
public sealed class JsParser(List<JsToken> tokens)
{
    private readonly List<IrDiagnostic> diagnostics = [];
    private readonly Stack<ContextKind> contextStack = new();
    private int position;

    private enum ContextKind
    {
        Call,
        Array,
        Object,
    }

    /// <summary>Parse errors collected during parsing.</summary>
    public IReadOnlyList<IrDiagnostic> Diagnostics => this.diagnostics;

    /// <summary>
    /// Parses the token stream into a program AST.
    /// </summary>
    public JsProgramNode Parse()
    {
        var statements = new List<JsStatementNode>();

        while (!this.IsAtEnd())
        {
            var stmt = this.ParseStatement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        return new JsProgramNode { Statements = statements, Line = 1, Column = 1 };
    }

    // ────────────────────────────────────────────────────────
    //  Statements
    // ────────────────────────────────────────────────────────
    private JsStatementNode? ParseStatement()
    {
        var current = this.Peek();

        return current.Kind switch
        {
            JsTokenKind.LeftBrace => this.ParseBlock(),
            JsTokenKind.VarKeyword or JsTokenKind.LetKeyword or JsTokenKind.ConstKeyword
                => this.ParseVariableDeclaration(),
            JsTokenKind.FunctionKeyword => this.ParseFunctionDeclaration(),
            JsTokenKind.IfKeyword => this.ParseIfStatement(),
            JsTokenKind.WhileKeyword => this.ParseWhileStatement(),
            JsTokenKind.DoKeyword => this.ParseDoWhileStatement(),
            JsTokenKind.ForKeyword => this.ParseForStatement(),
            JsTokenKind.SwitchKeyword => this.ParseSwitchStatement(),
            JsTokenKind.ReturnKeyword => this.ParseReturnStatement(),
            JsTokenKind.BreakKeyword => this.ParseBreakStatement(),
            JsTokenKind.ContinueKeyword => this.ParseContinueStatement(),
            JsTokenKind.Semicolon => this.ParseEmptyStatement(),
            _ => this.ParseExpressionStatement(),
        };
    }

    private JsBlockStatement ParseBlock()
    {
        var open = this.Expect(JsTokenKind.LeftBrace);
        var statements = new List<JsStatementNode>();

        while (!this.IsAtEnd() && this.Peek().Kind != JsTokenKind.RightBrace)
        {
            var stmt = this.ParseStatement();
            if (stmt is not null)
            {
                statements.Add(stmt);
            }
        }

        this.Expect(JsTokenKind.RightBrace);

        return new JsBlockStatement
        {
            Statements = statements,
            Line = open.Line,
            Column = open.Column,
        };
    }

    private JsVariableDeclaration ParseVariableDeclaration()
    {
        var keyword = this.Advance();
        var kind = keyword.Kind switch
        {
            JsTokenKind.VarKeyword => JsVariableKind.Var,
            JsTokenKind.LetKeyword => JsVariableKind.Let,
            JsTokenKind.ConstKeyword => JsVariableKind.Const,
            _ => JsVariableKind.Var,
        };

        var name = this.Expect(JsTokenKind.Identifier);
        JsExpressionNode? initializer = null;

        if (this.Peek().Kind == JsTokenKind.Assign)
        {
            this.Advance(); // skip '='
            initializer = this.ParseAssignmentExpression();
        }

        this.ConsumeSemicolon();

        return new JsVariableDeclaration
        {
            Kind = kind,
            Name = name.Text,
            Initializer = initializer,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private JsFunctionDeclaration ParseFunctionDeclaration()
    {
        var keyword = this.Expect(JsTokenKind.FunctionKeyword);
        var name = this.Expect(JsTokenKind.Identifier);
        this.Expect(JsTokenKind.LeftParen);

        var parameters = new List<string>();
        if (this.Peek().Kind != JsTokenKind.RightParen)
        {
            parameters.Add(this.Expect(JsTokenKind.Identifier).Text);
            while (this.Peek().Kind == JsTokenKind.Comma)
            {
                this.Advance(); // skip ','
                parameters.Add(this.Expect(JsTokenKind.Identifier).Text);
            }
        }

        this.Expect(JsTokenKind.RightParen);

        var body = this.ParseBlock();

        return new JsFunctionDeclaration
        {
            Name = name.Text,
            Parameters = parameters,
            Body = body,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private JsIfStatement ParseIfStatement()
    {
        var keyword = this.Expect(JsTokenKind.IfKeyword);
        this.Expect(JsTokenKind.LeftParen);
        var condition = this.ParseExpression();
        this.Expect(JsTokenKind.RightParen);

        var consequent = this.ParseStatement()!;
        JsStatementNode? alternate = null;

        if (this.Peek().Kind == JsTokenKind.ElseKeyword)
        {
            this.Advance(); // skip 'else'
            alternate = this.ParseStatement();
        }

        return new JsIfStatement
        {
            Condition = condition,
            Consequent = consequent,
            Alternate = alternate,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private JsWhileStatement ParseWhileStatement()
    {
        var keyword = this.Expect(JsTokenKind.WhileKeyword);
        this.Expect(JsTokenKind.LeftParen);
        var condition = this.ParseExpression();
        this.Expect(JsTokenKind.RightParen);

        var body = this.ParseStatement()!;

        return new JsWhileStatement
        {
            Condition = condition,
            Body = body,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private JsDoWhileStatement ParseDoWhileStatement()
    {
        var keyword = this.Expect(JsTokenKind.DoKeyword);
        var body = this.ParseStatement()!;
        this.Expect(JsTokenKind.WhileKeyword);
        this.Expect(JsTokenKind.LeftParen);
        var condition = this.ParseExpression();
        this.Expect(JsTokenKind.RightParen);
        this.ConsumeSemicolon();

        return new JsDoWhileStatement
        {
            Body = body,
            Condition = condition,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private JsStatementNode ParseForStatement()
    {
        var keyword = this.Expect(JsTokenKind.ForKeyword);
        this.Expect(JsTokenKind.LeftParen);

        // Check for for...of:  for (let/var/const x of expr)
        if (this.IsVarLetConst() && this.IsForOfAhead())
        {
            return this.ParseForOfStatement(keyword);
        }

        // Regular for statement
        JsStatementNode? init = null;
        if (this.Peek().Kind != JsTokenKind.Semicolon)
        {
            if (this.IsVarLetConst())
            {
                init = this.ParseForVarDecl();
            }
            else
            {
                init = new JsExpressionStatement
                {
                    Expression = this.ParseExpression(),
                    Line = this.Peek().Line,
                    Column = this.Peek().Column,
                };
                this.Expect(JsTokenKind.Semicolon);
            }
        }
        else
        {
            this.Advance(); // skip ';'
        }

        JsExpressionNode? condition = null;
        if (this.Peek().Kind != JsTokenKind.Semicolon)
        {
            condition = this.ParseExpression();
        }

        this.Expect(JsTokenKind.Semicolon);

        JsExpressionNode? update = null;
        if (this.Peek().Kind != JsTokenKind.RightParen)
        {
            update = this.ParseExpression();
        }

        this.Expect(JsTokenKind.RightParen);

        var body = this.ParseStatement()!;

        return new JsForStatement
        {
            Init = init,
            Condition = condition,
            Update = update,
            Body = body,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private JsVariableDeclaration ParseForVarDecl()
    {
        var keyword = this.Advance();
        var kind = keyword.Kind switch
        {
            JsTokenKind.VarKeyword => JsVariableKind.Var,
            JsTokenKind.LetKeyword => JsVariableKind.Let,
            JsTokenKind.ConstKeyword => JsVariableKind.Const,
            _ => JsVariableKind.Var,
        };

        var name = this.Expect(JsTokenKind.Identifier);
        JsExpressionNode? initializer = null;

        if (this.Peek().Kind == JsTokenKind.Assign)
        {
            this.Advance();
            initializer = this.ParseAssignmentExpression();
        }

        this.Expect(JsTokenKind.Semicolon);

        return new JsVariableDeclaration
        {
            Kind = kind,
            Name = name.Text,
            Initializer = initializer,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private JsForOfStatement ParseForOfStatement(JsToken keyword)
    {
        var varKeyword = this.Advance();
        var kind = varKeyword.Kind switch
        {
            JsTokenKind.VarKeyword => JsVariableKind.Var,
            JsTokenKind.LetKeyword => JsVariableKind.Let,
            JsTokenKind.ConstKeyword => JsVariableKind.Const,
            _ => JsVariableKind.Let,
        };

        var varName = this.Expect(JsTokenKind.Identifier);
        this.Expect(JsTokenKind.OfKeyword);
        var iterable = this.ParseAssignmentExpression();
        this.Expect(JsTokenKind.RightParen);
        var body = this.ParseStatement()!;

        return new JsForOfStatement
        {
            Kind = kind,
            Variable = varName.Text,
            Iterable = iterable,
            Body = body,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private JsSwitchStatement ParseSwitchStatement()
    {
        var keyword = this.Expect(JsTokenKind.SwitchKeyword);
        this.Expect(JsTokenKind.LeftParen);
        var discriminant = this.ParseExpression();
        this.Expect(JsTokenKind.RightParen);
        this.Expect(JsTokenKind.LeftBrace);

        var cases = new List<JsCaseClause>();
        while (!this.IsAtEnd() && this.Peek().Kind != JsTokenKind.RightBrace)
        {
            cases.Add(this.ParseCaseClause());
        }

        this.Expect(JsTokenKind.RightBrace);

        return new JsSwitchStatement
        {
            Discriminant = discriminant,
            Cases = cases,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private JsCaseClause ParseCaseClause()
    {
        var token = this.Peek();
        JsExpressionNode? test = null;

        if (token.Kind == JsTokenKind.CaseKeyword)
        {
            this.Advance();
            test = this.ParseExpression();
        }
        else
        {
            this.Expect(JsTokenKind.DefaultKeyword);
        }

        this.Expect(JsTokenKind.Colon);

        var body = new List<JsStatementNode>();
        while (!this.IsAtEnd()
            && this.Peek().Kind != JsTokenKind.CaseKeyword
            && this.Peek().Kind != JsTokenKind.DefaultKeyword
            && this.Peek().Kind != JsTokenKind.RightBrace)
        {
            var stmt = this.ParseStatement();
            if (stmt is not null)
            {
                body.Add(stmt);
            }
        }

        return new JsCaseClause
        {
            Test = test,
            Body = body,
            Line = token.Line,
            Column = token.Column,
        };
    }

    private JsReturnStatement ParseReturnStatement()
    {
        var keyword = this.Expect(JsTokenKind.ReturnKeyword);
        JsExpressionNode? value = null;

        // Return value is on the same line and not a semicolon/brace
        if (this.Peek().Kind != JsTokenKind.Semicolon
            && this.Peek().Kind != JsTokenKind.RightBrace
            && this.Peek().Kind != JsTokenKind.EndOfFile)
        {
            value = this.ParseExpression();
        }

        this.ConsumeSemicolon();

        return new JsReturnStatement
        {
            Value = value,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    private JsBreakStatement ParseBreakStatement()
    {
        var keyword = this.Expect(JsTokenKind.BreakKeyword);
        this.ConsumeSemicolon();
        return new JsBreakStatement { Line = keyword.Line, Column = keyword.Column };
    }

    private JsContinueStatement ParseContinueStatement()
    {
        var keyword = this.Expect(JsTokenKind.ContinueKeyword);
        this.ConsumeSemicolon();
        return new JsContinueStatement { Line = keyword.Line, Column = keyword.Column };
    }

    private JsExpressionStatement? ParseEmptyStatement()
    {
        this.Advance(); // skip ';'
        return null;
    }

    private JsExpressionStatement ParseExpressionStatement()
    {
        var expr = this.ParseExpression();
        this.ConsumeSemicolon();
        return new JsExpressionStatement
        {
            Expression = expr,
            Line = expr.Line,
            Column = expr.Column,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Expressions (precedence climbing)
    // ────────────────────────────────────────────────────────
    private JsExpressionNode ParseExpression()
    {
        var expr = this.ParseAssignmentExpression();

        // Comma operator
        if (this.Peek().Kind == JsTokenKind.Comma
            && !this.IsInContext(ContextKind.Call)
            && !this.IsInContext(ContextKind.Array)
            && !this.IsInContext(ContextKind.Object))
        {
            var exprs = new List<JsExpressionNode> { expr };
            while (this.Peek().Kind == JsTokenKind.Comma)
            {
                this.Advance();
                exprs.Add(this.ParseAssignmentExpression());
            }

            return new JsCommaExpression { Expressions = exprs, Line = expr.Line, Column = expr.Column };
        }

        return expr;
    }

    private JsExpressionNode ParseAssignmentExpression()
    {
        var expr = this.ParseTernaryExpression();

        var assignOp = this.TryParseAssignmentOperator();
        if (assignOp is not null)
        {
            var value = this.ParseAssignmentExpression(); // right-associative
            return new JsAssignmentExpression
            {
                Target = expr,
                Operator = assignOp.Value,
                Value = value,
                Line = expr.Line,
                Column = expr.Column,
            };
        }

        return expr;
    }

    private JsAssignmentOperator? TryParseAssignmentOperator()
    {
        var kind = this.Peek().Kind;
        JsAssignmentOperator? op = kind switch
        {
            JsTokenKind.Assign => JsAssignmentOperator.Assign,
            JsTokenKind.PlusEqual => JsAssignmentOperator.AddAssign,
            JsTokenKind.MinusEqual => JsAssignmentOperator.SubAssign,
            JsTokenKind.StarEqual => JsAssignmentOperator.MulAssign,
            JsTokenKind.SlashEqual => JsAssignmentOperator.DivAssign,
            JsTokenKind.PercentEqual => JsAssignmentOperator.ModAssign,
            JsTokenKind.AmpEqual => JsAssignmentOperator.AndAssign,
            JsTokenKind.PipeEqual => JsAssignmentOperator.OrAssign,
            JsTokenKind.CaretEqual => JsAssignmentOperator.XorAssign,
            JsTokenKind.ShiftLeftEqual => JsAssignmentOperator.ShlAssign,
            JsTokenKind.ShiftRightEqual => JsAssignmentOperator.ShrAssign,
            JsTokenKind.UnsignedShiftRightEqual => JsAssignmentOperator.UnsignedShrAssign,
            _ => null,
        };

        if (op is not null)
        {
            this.Advance();
        }

        return op;
    }

    private JsExpressionNode ParseTernaryExpression()
    {
        var expr = this.ParseLogicalOrExpression();

        if (this.Peek().Kind == JsTokenKind.Question)
        {
            this.Advance(); // skip '?'
            var consequent = this.ParseAssignmentExpression();
            this.Expect(JsTokenKind.Colon);
            var alternate = this.ParseAssignmentExpression();

            return new JsTernaryExpression
            {
                Condition = expr,
                Consequent = consequent,
                Alternate = alternate,
                Line = expr.Line,
                Column = expr.Column,
            };
        }

        return expr;
    }

    private JsExpressionNode ParseLogicalOrExpression()
    {
        var left = this.ParseLogicalAndExpression();

        while (this.Peek().Kind == JsTokenKind.LogicalOr)
        {
            this.Advance();
            var right = this.ParseLogicalAndExpression();
            left = new JsBinaryExpression
            {
                Left = left,
                Operator = JsBinaryOperator.LogicalOr,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private JsExpressionNode ParseLogicalAndExpression()
    {
        var left = this.ParseBitwiseOrExpression();

        while (this.Peek().Kind == JsTokenKind.LogicalAnd)
        {
            this.Advance();
            var right = this.ParseBitwiseOrExpression();
            left = new JsBinaryExpression
            {
                Left = left,
                Operator = JsBinaryOperator.LogicalAnd,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private JsExpressionNode ParseBitwiseOrExpression()
    {
        var left = this.ParseBitwiseXorExpression();

        while (this.Peek().Kind == JsTokenKind.Pipe)
        {
            this.Advance();
            var right = this.ParseBitwiseXorExpression();
            left = new JsBinaryExpression
            {
                Left = left,
                Operator = JsBinaryOperator.BitwiseOr,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private JsExpressionNode ParseBitwiseXorExpression()
    {
        var left = this.ParseBitwiseAndExpression();

        while (this.Peek().Kind == JsTokenKind.Caret)
        {
            this.Advance();
            var right = this.ParseBitwiseAndExpression();
            left = new JsBinaryExpression
            {
                Left = left,
                Operator = JsBinaryOperator.BitwiseXor,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private JsExpressionNode ParseBitwiseAndExpression()
    {
        var left = this.ParseEqualityExpression();

        while (this.Peek().Kind == JsTokenKind.Ampersand)
        {
            this.Advance();
            var right = this.ParseEqualityExpression();
            left = new JsBinaryExpression
            {
                Left = left,
                Operator = JsBinaryOperator.BitwiseAnd,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private JsExpressionNode ParseEqualityExpression()
    {
        var left = this.ParseRelationalExpression();

        while (true)
        {
            var op = this.Peek().Kind switch
            {
                JsTokenKind.Equal => JsBinaryOperator.Equal,
                JsTokenKind.NotEqual => JsBinaryOperator.NotEqual,
                JsTokenKind.StrictEqual => JsBinaryOperator.StrictEqual,
                JsTokenKind.StrictNotEqual => JsBinaryOperator.StrictNotEqual,
                _ => (JsBinaryOperator?)null,
            };

            if (op is null)
            {
                break;
            }

            this.Advance();
            var right = this.ParseRelationalExpression();
            left = new JsBinaryExpression
            {
                Left = left,
                Operator = op.Value,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private JsExpressionNode ParseRelationalExpression()
    {
        var left = this.ParseShiftExpression();

        while (true)
        {
            var op = this.Peek().Kind switch
            {
                JsTokenKind.Less => JsBinaryOperator.Less,
                JsTokenKind.LessEqual => JsBinaryOperator.LessEqual,
                JsTokenKind.Greater => JsBinaryOperator.Greater,
                JsTokenKind.GreaterEqual => JsBinaryOperator.GreaterEqual,
                _ => (JsBinaryOperator?)null,
            };

            if (op is null)
            {
                break;
            }

            this.Advance();
            var right = this.ParseShiftExpression();
            left = new JsBinaryExpression
            {
                Left = left,
                Operator = op.Value,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private JsExpressionNode ParseShiftExpression()
    {
        var left = this.ParseAdditiveExpression();

        while (true)
        {
            var op = this.Peek().Kind switch
            {
                JsTokenKind.ShiftLeft => JsBinaryOperator.Shl,
                JsTokenKind.ShiftRight => JsBinaryOperator.Shr,
                JsTokenKind.UnsignedShiftRight => JsBinaryOperator.UnsignedShr,
                _ => (JsBinaryOperator?)null,
            };

            if (op is null)
            {
                break;
            }

            this.Advance();
            var right = this.ParseAdditiveExpression();
            left = new JsBinaryExpression
            {
                Left = left,
                Operator = op.Value,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private JsExpressionNode ParseAdditiveExpression()
    {
        var left = this.ParseMultiplicativeExpression();

        while (this.Peek().Kind is JsTokenKind.Plus or JsTokenKind.Minus)
        {
            var op = this.Advance().Kind == JsTokenKind.Plus
                ? JsBinaryOperator.Add
                : JsBinaryOperator.Sub;
            var right = this.ParseMultiplicativeExpression();
            left = new JsBinaryExpression
            {
                Left = left,
                Operator = op,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private JsExpressionNode ParseMultiplicativeExpression()
    {
        var left = this.ParseUnaryExpression();

        while (this.Peek().Kind is JsTokenKind.Asterisk or JsTokenKind.Slash or JsTokenKind.Percent)
        {
            var opToken = this.Advance();
            var op = opToken.Kind switch
            {
                JsTokenKind.Asterisk => JsBinaryOperator.Mul,
                JsTokenKind.Slash => JsBinaryOperator.Div,
                _ => JsBinaryOperator.Mod,
            };

            var right = this.ParseUnaryExpression();
            left = new JsBinaryExpression
            {
                Left = left,
                Operator = op,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private JsExpressionNode ParseUnaryExpression()
    {
        var current = this.Peek();

        // Prefix increment/decrement
        if (current.Kind is JsTokenKind.PlusPlus or JsTokenKind.MinusMinus)
        {
            this.Advance();
            var operand = this.ParseUnaryExpression();
            return new JsUpdateExpression
            {
                Operand = operand,
                IsIncrement = current.Kind == JsTokenKind.PlusPlus,
                IsPrefix = true,
                Line = current.Line,
                Column = current.Column,
            };
        }

        // typeof
        if (current.Kind == JsTokenKind.TypeofKeyword)
        {
            this.Advance();
            var operand = this.ParseUnaryExpression();
            return new JsUnaryExpression
            {
                Operator = JsUnaryOperator.Typeof,
                Operand = operand,
                Line = current.Line,
                Column = current.Column,
            };
        }

        // Unary operators: -, +, !, ~
        if (current.Kind is JsTokenKind.Minus or JsTokenKind.Plus
            or JsTokenKind.Exclamation or JsTokenKind.Tilde)
        {
            this.Advance();
            var operand = this.ParseUnaryExpression();
            var op = current.Kind switch
            {
                JsTokenKind.Minus => JsUnaryOperator.Negate,
                JsTokenKind.Plus => JsUnaryOperator.Plus,
                JsTokenKind.Exclamation => JsUnaryOperator.LogicalNot,
                _ => JsUnaryOperator.BitwiseNot,
            };

            return new JsUnaryExpression
            {
                Operator = op,
                Operand = operand,
                Line = current.Line,
                Column = current.Column,
            };
        }

        return this.ParsePostfixExpression();
    }

    private JsExpressionNode ParsePostfixExpression()
    {
        var expr = this.ParseCallExpression();

        // Postfix ++/--
        if (this.Peek().Kind is JsTokenKind.PlusPlus or JsTokenKind.MinusMinus)
        {
            var opToken = this.Advance();
            return new JsUpdateExpression
            {
                Operand = expr,
                IsIncrement = opToken.Kind == JsTokenKind.PlusPlus,
                IsPrefix = false,
                Line = expr.Line,
                Column = expr.Column,
            };
        }

        return expr;
    }

    private JsExpressionNode ParseCallExpression()
    {
        var expr = this.ParsePrimaryExpression();

        // Chain: member access, array access, function calls
        while (true)
        {
            if (this.Peek().Kind == JsTokenKind.Dot)
            {
                this.Advance(); // skip '.'
                var prop = this.Expect(JsTokenKind.Identifier);
                expr = new JsMemberExpression
                {
                    Object = expr,
                    Property = prop.Text,
                    Line = expr.Line,
                    Column = expr.Column,
                };
            }
            else if (this.Peek().Kind == JsTokenKind.LeftBracket)
            {
                this.Advance(); // skip '['
                var index = this.ParseExpression();
                this.Expect(JsTokenKind.RightBracket);
                expr = new JsArrayAccessExpression
                {
                    Array = expr,
                    Index = index,
                    Line = expr.Line,
                    Column = expr.Column,
                };
            }
            else if (this.Peek().Kind == JsTokenKind.LeftParen)
            {
                this.Advance(); // skip '('
                this.PushContext(ContextKind.Call);
                var args = new List<JsExpressionNode>();
                if (this.Peek().Kind != JsTokenKind.RightParen)
                {
                    args.Add(this.ParseAssignmentExpression());
                    while (this.Peek().Kind == JsTokenKind.Comma)
                    {
                        this.Advance();
                        args.Add(this.ParseAssignmentExpression());
                    }
                }

                this.PopContext();
                this.Expect(JsTokenKind.RightParen);
                expr = new JsCallExpression
                {
                    Callee = expr,
                    Arguments = args,
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

    private JsExpressionNode ParsePrimaryExpression()
    {
        var current = this.Peek();

        switch (current.Kind)
        {
            case JsTokenKind.IntegerLiteral:
                this.Advance();
                return new JsIntegerLiteral
                {
                    Value = current.IntValue,
                    Line = current.Line,
                    Column = current.Column,
                };

            case JsTokenKind.StringLiteral:
                this.Advance();
                return new JsStringLiteral
                {
                    Value = current.StringValue ?? string.Empty,
                    Line = current.Line,
                    Column = current.Column,
                };

            case JsTokenKind.TemplateLiteralFull:
                return this.ParseTemplateLiteralExpression();

            case JsTokenKind.TrueKeyword:
                this.Advance();
                return new JsBooleanLiteral { Value = true, Line = current.Line, Column = current.Column };

            case JsTokenKind.FalseKeyword:
                this.Advance();
                return new JsBooleanLiteral { Value = false, Line = current.Line, Column = current.Column };

            case JsTokenKind.NullKeyword:
                this.Advance();
                return new JsNullLiteral { Line = current.Line, Column = current.Column };

            case JsTokenKind.UndefinedKeyword:
                this.Advance();
                return new JsUndefinedLiteral { Line = current.Line, Column = current.Column };

            case JsTokenKind.Identifier:
                return this.ParseIdentifierOrArrow();

            case JsTokenKind.LeftParen:
                return this.ParseParenOrArrow();

            case JsTokenKind.LeftBracket:
                return this.ParseArrayLiteral();

            case JsTokenKind.LeftBrace:
                return this.ParseObjectLiteral();

            case JsTokenKind.FunctionKeyword:
                return this.ParseFunctionExpression();

            default:
                this.diagnostics.Add(new IrDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Unexpected token '{current.Text}'.",
                    Line = current.Line,
                    Column = current.Column,
                });
                this.Advance(); // skip to avoid infinite loop
                return new JsIntegerLiteral { Value = 0, Line = current.Line, Column = current.Column };
        }
    }

    private JsExpressionNode ParseTemplateLiteralExpression()
    {
        var token = this.Advance();
        var raw = token.StringValue ?? string.Empty;

        // Parse ${...} interpolations from the raw template text
        var parts = new List<JsTemplatePart>();
        var buffer = new System.Text.StringBuilder();
        var i = 0;

        while (i < raw.Length)
        {
            if (i + 1 < raw.Length && raw[i] == '$' && raw[i + 1] == '{')
            {
                // Flush accumulated text
                if (buffer.Length > 0)
                {
                    parts.Add(new JsTemplateString { Value = buffer.ToString(), Line = token.Line, Column = token.Column });
                    buffer.Clear();
                }

                // Find matching closing brace (simple — no nested braces)
                i += 2; // skip ${
                var exprStart = i;
                var depth = 1;
                while (i < raw.Length && depth > 0)
                {
                    if (raw[i] == '{')
                    {
                        depth++;
                    }
                    else if (raw[i] == '}')
                    {
                        depth--;
                    }

                    if (depth > 0)
                    {
                        i++;
                    }
                }

                var exprText = raw[exprStart..i];
                i++; // skip closing }

                // Lex and parse the embedded expression
                var exprTokens = new JsLexer(exprText).Tokenize();
                var exprParser = new JsParser(exprTokens);
                var expr = exprParser.ParseExpression();
                this.diagnostics.AddRange(exprParser.Diagnostics);

                parts.Add(new JsTemplateExpression { Expression = expr, Line = token.Line, Column = token.Column });
            }
            else
            {
                buffer.Append(raw[i]);
                i++;
            }
        }

        // Flush remaining text
        if (buffer.Length > 0)
        {
            parts.Add(new JsTemplateString { Value = buffer.ToString(), Line = token.Line, Column = token.Column });
        }

        // If no interpolations, treat as a simple string
        if (parts.Count == 1 && parts[0] is JsTemplateString singleStr)
        {
            return new JsStringLiteral { Value = singleStr.Value, Line = token.Line, Column = token.Column };
        }

        return new JsTemplateLiteral { Parts = parts, Line = token.Line, Column = token.Column };
    }

    private JsExpressionNode ParseIdentifierOrArrow()
    {
        // Single-param arrow: x => expr
        if (this.position + 1 < tokens.Count && tokens[this.position + 1].Kind == JsTokenKind.Arrow)
        {
            var param = this.Advance(); // identifier
            this.Advance(); // skip '=>'
            return this.ParseArrowBody([param.Text], param);
        }

        var token = this.Advance();
        return new JsIdentifierExpression
        {
            Name = token.Text,
            Line = token.Line,
            Column = token.Column,
        };
    }

    private JsExpressionNode ParseParenOrArrow()
    {
        // Try to detect arrow function: (...) =>
        if (this.IsArrowFunction())
        {
            return this.ParseArrowFunction();
        }

        // Regular parenthesised expression
        this.Advance(); // skip '('
        var expr = this.ParseExpression();
        this.Expect(JsTokenKind.RightParen);
        return expr;
    }

    private JsArrowFunctionExpression ParseArrowFunction()
    {
        var open = this.Expect(JsTokenKind.LeftParen);
        var parameters = new List<string>();

        if (this.Peek().Kind != JsTokenKind.RightParen)
        {
            parameters.Add(this.Expect(JsTokenKind.Identifier).Text);
            while (this.Peek().Kind == JsTokenKind.Comma)
            {
                this.Advance();
                parameters.Add(this.Expect(JsTokenKind.Identifier).Text);
            }
        }

        this.Expect(JsTokenKind.RightParen);
        this.Expect(JsTokenKind.Arrow);

        return this.ParseArrowBody(parameters, open);
    }

    private JsArrowFunctionExpression ParseArrowBody(IReadOnlyList<string> parameters, JsToken startToken)
    {
        if (this.Peek().Kind == JsTokenKind.LeftBrace)
        {
            var body = this.ParseBlock();
            return new JsArrowFunctionExpression
            {
                Parameters = parameters,
                Body = body,
                Line = startToken.Line,
                Column = startToken.Column,
            };
        }

        var expr = this.ParseAssignmentExpression();
        return new JsArrowFunctionExpression
        {
            Parameters = parameters,
            Expression = expr,
            Line = startToken.Line,
            Column = startToken.Column,
        };
    }

    private JsArrayLiteral ParseArrayLiteral()
    {
        var open = this.Expect(JsTokenKind.LeftBracket);
        this.PushContext(ContextKind.Array);
        var elements = new List<JsExpressionNode>();

        if (this.Peek().Kind != JsTokenKind.RightBracket)
        {
            elements.Add(this.ParseAssignmentExpression());
            while (this.Peek().Kind == JsTokenKind.Comma)
            {
                this.Advance();
                if (this.Peek().Kind == JsTokenKind.RightBracket)
                {
                    break; // trailing comma
                }

                elements.Add(this.ParseAssignmentExpression());
            }
        }

        this.PopContext();
        this.Expect(JsTokenKind.RightBracket);

        return new JsArrayLiteral
        {
            Elements = elements,
            Line = open.Line,
            Column = open.Column,
        };
    }

    private JsObjectLiteral ParseObjectLiteral()
    {
        var open = this.Expect(JsTokenKind.LeftBrace);
        this.PushContext(ContextKind.Object);
        var properties = new List<JsPropertyNode>();

        if (this.Peek().Kind != JsTokenKind.RightBrace)
        {
            properties.Add(this.ParseProperty());
            while (this.Peek().Kind == JsTokenKind.Comma)
            {
                this.Advance();
                if (this.Peek().Kind == JsTokenKind.RightBrace)
                {
                    break; // trailing comma
                }

                properties.Add(this.ParseProperty());
            }
        }

        this.PopContext();
        this.Expect(JsTokenKind.RightBrace);

        return new JsObjectLiteral
        {
            Properties = properties,
            Line = open.Line,
            Column = open.Column,
        };
    }

    private JsPropertyNode ParseProperty()
    {
        var key = this.Expect(JsTokenKind.Identifier);

        if (this.Peek().Kind == JsTokenKind.Colon)
        {
            this.Advance(); // skip ':'
            var value = this.ParseAssignmentExpression();
            return new JsPropertyNode
            {
                Key = key.Text,
                Value = value,
                Line = key.Line,
                Column = key.Column,
            };
        }

        // Shorthand: { x } means { x: x }
        return new JsPropertyNode
        {
            Key = key.Text,
            Value = new JsIdentifierExpression { Name = key.Text, Line = key.Line, Column = key.Column },
            Line = key.Line,
            Column = key.Column,
        };
    }

    private JsArrowFunctionExpression ParseFunctionExpression()
    {
        // function(params) { body } — treated as arrow for AST simplicity
        var keyword = this.Expect(JsTokenKind.FunctionKeyword);

        // Optional name (ignored for function expressions)
        if (this.Peek().Kind == JsTokenKind.Identifier)
        {
            this.Advance();
        }

        this.Expect(JsTokenKind.LeftParen);
        var parameters = new List<string>();

        if (this.Peek().Kind != JsTokenKind.RightParen)
        {
            parameters.Add(this.Expect(JsTokenKind.Identifier).Text);
            while (this.Peek().Kind == JsTokenKind.Comma)
            {
                this.Advance();
                parameters.Add(this.Expect(JsTokenKind.Identifier).Text);
            }
        }

        this.Expect(JsTokenKind.RightParen);
        var body = this.ParseBlock();

        return new JsArrowFunctionExpression
        {
            Parameters = parameters,
            Body = body,
            Line = keyword.Line,
            Column = keyword.Column,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Lookahead helpers
    // ────────────────────────────────────────────────────────
    private bool IsArrowFunction()
    {
        // Simple heuristic: scan from '(' forward looking for ') =>'
        var saved = this.position;
        if (tokens[saved].Kind != JsTokenKind.LeftParen)
        {
            return false;
        }

        var depth = 1;
        var i = saved + 1;
        while (i < tokens.Count && depth > 0)
        {
            if (tokens[i].Kind == JsTokenKind.LeftParen)
            {
                depth++;
            }
            else if (tokens[i].Kind == JsTokenKind.RightParen)
            {
                depth--;
            }

            i++;
        }

        return i < tokens.Count && tokens[i].Kind == JsTokenKind.Arrow;
    }

    private bool IsVarLetConst()
    {
        return this.Peek().Kind is JsTokenKind.VarKeyword
            or JsTokenKind.LetKeyword
            or JsTokenKind.ConstKeyword;
    }

    private bool IsForOfAhead()
    {
        // Lookahead: var/let/const IDENTIFIER of ...
        return this.position + 2 < tokens.Count
            && tokens[this.position + 1].Kind == JsTokenKind.Identifier
            && tokens[this.position + 2].Kind == JsTokenKind.OfKeyword;
    }

    // ────────────────────────────────────────────────────────
    //  Context tracking (for comma disambiguation)
    // ────────────────────────────────────────────────────────
    private void PushContext(ContextKind kind) => this.contextStack.Push(kind);

    private void PopContext() => this.contextStack.Pop();

    private bool IsInContext(ContextKind kind)
    {
        return this.contextStack.Contains(kind);
    }

    // ────────────────────────────────────────────────────────
    //  Token manipulation
    // ────────────────────────────────────────────────────────
    private JsToken Peek()
    {
        return this.position < tokens.Count
            ? tokens[this.position]
            : new JsToken { Kind = JsTokenKind.EndOfFile, Text = string.Empty, Line = 0, Column = 0 };
    }

    private JsToken Advance()
    {
        var token = this.Peek();
        this.position++;
        return token;
    }

    private JsToken Expect(JsTokenKind kind)
    {
        var token = this.Peek();
        if (token.Kind != kind)
        {
            this.diagnostics.Add(new IrDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Message = $"Expected '{kind}' but found '{token.Text}'.",
                Line = token.Line,
                Column = token.Column,
            });

            // Return the current token as a best-effort recovery
            return token;
        }

        return this.Advance();
    }

    private void ConsumeSemicolon()
    {
        // JavaScript allows optional semicolons — consume if present
        if (this.Peek().Kind == JsTokenKind.Semicolon)
        {
            this.Advance();
        }
    }

    private bool IsAtEnd()
    {
        return this.position >= tokens.Count || this.Peek().Kind == JsTokenKind.EndOfFile;
    }
}
