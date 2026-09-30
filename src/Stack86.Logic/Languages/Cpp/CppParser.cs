namespace Stack86.Logic.Languages.Cpp;

using Stack86.Common.Exceptions;

using Stack86.Logic.Languages.Cpp.Ast;

/// <summary>
/// Recursive-descent parser for a subset of C++.
/// Produces a <see cref="CppProgramNode"/> AST that is later transpiled to C.
/// </summary>
public sealed class CppParser(List<CppToken> tokens)
{
    private readonly HashSet<string> knownClassNames = [];
    private readonly HashSet<string> knownTemplateParams = [];
    private int position;

    /// <summary>
    /// Parses the token stream into a <see cref="CppProgramNode"/>.
    /// </summary>
    public CppProgramNode Parse()
    {
        var declarations = new List<CppDeclarationNode>();

        while (!this.IsAtEnd())
        {
            declarations.Add(this.ParseTopLevelDeclaration());
        }

        return new CppProgramNode { Declarations = declarations };
    }

    // ────────────────────────────────────────────────────────
    //  Private static helpers
    // ────────────────────────────────────────────────────────
    private static bool IsReferenceType()
    {
        // Heuristic: & followed by ) or , is address-of or bitwise-and, that's fine.
        // This method checks for ambiguity — in expression context & is always bitwise-and or address-of.
        return false;
    }

    // ────────────────────────────────────────────────────────
    //  Top-level declarations
    // ────────────────────────────────────────────────────────
    private CppDeclarationNode ParseTopLevelDeclaration()
    {
        // using namespace std;
        if (this.Check(CppTokenKind.UsingKeyword))
        {
            return this.ParseUsingDirective();
        }

        // namespace Foo { ... }
        if (this.Check(CppTokenKind.NamespaceKeyword))
        {
            return this.ParseNamespaceDeclaration();
        }

        // template<typename T> ...
        if (this.Check(CppTokenKind.TemplateKeyword))
        {
            return this.ParseTemplateDeclaration();
        }

        // class Foo { ... };
        if (this.Check(CppTokenKind.ClassKeyword) && this.IsClassDeclaration())
        {
            return this.ParseClassDeclaration();
        }

        // struct Foo { ... };
        if (this.Check(CppTokenKind.StructKeyword) && this.IsClassOrStructDeclaration())
        {
            return this.ParseStructAsClassDeclaration();
        }

        // enum Name { ... };
        if (this.Check(CppTokenKind.EnumKeyword))
        {
            return this.ParseEnumDeclaration();
        }

        // Function or global variable
        return this.ParseFunctionOrVariable();
    }

    // ────────────────────────────────────────────────────────
    //  Using directive
    // ────────────────────────────────────────────────────────
    private CppUsingDirective ParseUsingDirective()
    {
        var start = this.Expect(CppTokenKind.UsingKeyword);
        this.Expect(CppTokenKind.NamespaceKeyword);
        var name = this.Expect(CppTokenKind.Identifier);

        // Handle nested namespaces: std::chrono etc
        var fullName = name.Text;
        while (this.Match(CppTokenKind.ScopeResolution))
        {
            var next = this.Expect(CppTokenKind.Identifier);
            fullName += "::" + next.Text;
        }

        this.Expect(CppTokenKind.Semicolon);
        return new CppUsingDirective { NamespaceName = fullName, Line = start.Line, Column = start.Column };
    }

    // ────────────────────────────────────────────────────────
    //  Namespace
    // ────────────────────────────────────────────────────────
    private CppNamespaceDeclaration ParseNamespaceDeclaration()
    {
        var start = this.Expect(CppTokenKind.NamespaceKeyword);
        var name = this.Expect(CppTokenKind.Identifier);
        this.Expect(CppTokenKind.LeftBrace);

        var declarations = new List<CppDeclarationNode>();
        while (!this.Check(CppTokenKind.RightBrace) && !this.IsAtEnd())
        {
            declarations.Add(this.ParseTopLevelDeclaration());
        }

        this.Expect(CppTokenKind.RightBrace);
        return new CppNamespaceDeclaration
        {
            Name = name.Text,
            Declarations = declarations,
            Line = start.Line,
            Column = start.Column,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Template
    // ────────────────────────────────────────────────────────
    private CppTemplateDeclaration ParseTemplateDeclaration()
    {
        var start = this.Expect(CppTokenKind.TemplateKeyword);
        this.Expect(CppTokenKind.Less);

        var typeParams = new List<string>();
        do
        {
            // template<typename T> or template<class T>
            if (this.Check(CppTokenKind.TypenameKeyword) || this.Check(CppTokenKind.ClassKeyword))
            {
                this.Advance();
            }

            var param = this.Expect(CppTokenKind.Identifier);
            typeParams.Add(param.Text);
            this.knownTemplateParams.Add(param.Text);
        }
        while (this.Match(CppTokenKind.Comma));

        this.Expect(CppTokenKind.Greater);

        var inner = this.ParseTopLevelDeclaration();

        foreach (var tp in typeParams)
        {
            this.knownTemplateParams.Remove(tp);
        }

        return new CppTemplateDeclaration
        {
            TypeParameters = typeParams,
            Inner = inner,
            Line = start.Line,
            Column = start.Column,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Class declaration
    // ────────────────────────────────────────────────────────
    private bool IsClassDeclaration()
    {
        // class <Identifier> { or class <Identifier> :
        var la = this.position + 1;
        return la < tokens.Count && tokens[la].Kind == CppTokenKind.Identifier;
    }

    private bool IsClassOrStructDeclaration()
    {
        var la = this.position + 1;
        if (la >= tokens.Count || tokens[la].Kind != CppTokenKind.Identifier)
        {
            return false;
        }

        var la2 = la + 1;
        return la2 < tokens.Count &&
            (tokens[la2].Kind == CppTokenKind.LeftBrace || tokens[la2].Kind == CppTokenKind.Colon);
    }

    private CppClassDeclaration ParseClassDeclaration()
    {
        var start = this.Expect(CppTokenKind.ClassKeyword);
        return this.ParseClassBody(start, CppAccessModifier.Private);
    }

    private CppClassDeclaration ParseStructAsClassDeclaration()
    {
        var start = this.Expect(CppTokenKind.StructKeyword);
        return this.ParseClassBody(start, CppAccessModifier.Public);
    }

    private CppClassDeclaration ParseClassBody(CppToken start, CppAccessModifier defaultAccess)
    {
        var name = this.Expect(CppTokenKind.Identifier);
        this.knownClassNames.Add(name.Text);

        // Optional inheritance: : public Base
        string? baseClass = null;
        var baseAccess = CppAccessModifier.Private;
        if (this.Match(CppTokenKind.Colon))
        {
            baseAccess = this.TryParseAccessModifier() ?? defaultAccess;
            var baseName = this.Expect(CppTokenKind.Identifier);
            baseClass = baseName.Text;
        }

        this.Expect(CppTokenKind.LeftBrace);

        var sections = new List<CppAccessSection>();
        var currentAccess = defaultAccess;
        var currentMembers = new List<CppDeclarationNode>();

        while (!this.Check(CppTokenKind.RightBrace) && !this.IsAtEnd())
        {
            // Access label: public: / private: / protected:
            var accessMod = this.TryParseAccessLabel();
            if (accessMod.HasValue)
            {
                if (currentMembers.Count > 0)
                {
                    sections.Add(new CppAccessSection
                    {
                        Access = currentAccess,
                        Members = currentMembers,
                        Line = start.Line,
                        Column = start.Column,
                    });
                    currentMembers = [];
                }

                currentAccess = accessMod.Value;
                continue;
            }

            currentMembers.Add(this.ParseClassMember(name.Text));
        }

        if (currentMembers.Count > 0)
        {
            sections.Add(new CppAccessSection
            {
                Access = currentAccess,
                Members = currentMembers,
                Line = start.Line,
                Column = start.Column,
            });
        }

        this.Expect(CppTokenKind.RightBrace);
        this.Expect(CppTokenKind.Semicolon);

        return new CppClassDeclaration
        {
            Name = name.Text,
            BaseClassName = baseClass,
            BaseAccess = baseAccess,
            Sections = sections,
            Line = start.Line,
            Column = start.Column,
        };
    }

    private CppDeclarationNode ParseClassMember(string className)
    {
        // Constructor: ClassName(...)
        if (this.Check(CppTokenKind.Identifier) && this.Peek().Text == className && this.IsConstructor())
        {
            return this.ParseConstructor(className);
        }

        // Destructor: ~ClassName()
        if (this.Check(CppTokenKind.Tilde))
        {
            return this.ParseDestructor(className);
        }

        // Virtual method
        var isVirtual = this.Match(CppTokenKind.VirtualKeyword);

        // Parse type and name
        var type = this.ParseType();
        var memberName = this.Expect(CppTokenKind.Identifier);

        // Method: type name(...)
        if (this.Check(CppTokenKind.LeftParen))
        {
            return this.ParseMethodDeclaration(type, memberName, isVirtual);
        }

        // Field: type name;  or  type name = expr;
        CppExpressionNode? init = null;
        if (this.Match(CppTokenKind.Assign))
        {
            init = this.ParseExpression();
        }

        this.Expect(CppTokenKind.Semicolon);
        return new CppFieldDeclaration
        {
            Type = type,
            Name = memberName.Text,
            Initializer = init,
            Line = memberName.Line,
            Column = memberName.Column,
        };
    }

    private bool IsConstructor()
    {
        // ClassName followed by (
        var la = this.position + 1;
        return la < tokens.Count && tokens[la].Kind == CppTokenKind.LeftParen;
    }

    private CppConstructorDeclaration ParseConstructor(string className)
    {
        var name = this.Expect(CppTokenKind.Identifier);
        this.Expect(CppTokenKind.LeftParen);
        var parameters = this.ParseParameterList();
        this.Expect(CppTokenKind.RightParen);

        // Member initializer list: : x(val), y(val)
        var initializers = new List<CppMemberInitializer>();
        if (this.Match(CppTokenKind.Colon))
        {
            do
            {
                var memberNameTok = this.Expect(CppTokenKind.Identifier);
                this.Expect(CppTokenKind.LeftParen);
                var args = new List<CppExpressionNode>();
                if (!this.Check(CppTokenKind.RightParen))
                {
                    do
                    {
                        args.Add(this.ParseAssignment());
                    }
                    while (this.Match(CppTokenKind.Comma));
                }

                this.Expect(CppTokenKind.RightParen);
                initializers.Add(new CppMemberInitializer
                {
                    MemberName = memberNameTok.Text,
                    Arguments = args,
                    Line = memberNameTok.Line,
                    Column = memberNameTok.Column,
                });
            }
            while (this.Match(CppTokenKind.Comma));
        }

        // Body or semicolon
        CppBlockStatement? body = null;
        if (this.Check(CppTokenKind.LeftBrace))
        {
            body = this.ParseBlock();
        }
        else
        {
            this.Expect(CppTokenKind.Semicolon);
        }

        return new CppConstructorDeclaration
        {
            ClassName = className,
            Parameters = parameters,
            Initializers = initializers,
            Body = body,
            Line = name.Line,
            Column = name.Column,
        };
    }

    private CppDestructorDeclaration ParseDestructor(string className)
    {
        var start = this.Expect(CppTokenKind.Tilde);
        this.Expect(CppTokenKind.Identifier); // class name
        this.Expect(CppTokenKind.LeftParen);
        this.Expect(CppTokenKind.RightParen);

        CppBlockStatement? body = null;
        if (this.Check(CppTokenKind.LeftBrace))
        {
            body = this.ParseBlock();
        }
        else
        {
            this.Expect(CppTokenKind.Semicolon);
        }

        return new CppDestructorDeclaration
        {
            ClassName = className,
            Body = body,
            Line = start.Line,
            Column = start.Column,
        };
    }

    private CppMethodDeclaration ParseMethodDeclaration(CppTypeNode returnType, CppToken name, bool isVirtual)
    {
        this.Expect(CppTokenKind.LeftParen);
        var parameters = this.ParseParameterList();
        this.Expect(CppTokenKind.RightParen);

        var isConst = this.Match(CppTokenKind.ConstKeyword);
        var isOverride = this.Match(CppTokenKind.OverrideKeyword);

        CppBlockStatement? body = null;
        if (this.Check(CppTokenKind.LeftBrace))
        {
            body = this.ParseBlock();
        }
        else
        {
            this.Expect(CppTokenKind.Semicolon);
        }

        return new CppMethodDeclaration
        {
            ReturnType = returnType,
            Name = name.Text,
            Parameters = parameters,
            Body = body,
            IsVirtual = isVirtual,
            IsOverride = isOverride,
            IsConst = isConst,
            Line = name.Line,
            Column = name.Column,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Enum declaration
    // ────────────────────────────────────────────────────────
    private CppVariableDeclaration ParseEnumDeclaration()
    {
        var start = this.Expect(CppTokenKind.EnumKeyword);
        var name = this.Expect(CppTokenKind.Identifier);
        this.Expect(CppTokenKind.LeftBrace);

        // Build a comma-separated literal string for C passthrough
        // We represent enum as a typedef int + #define constants in the emitter
        var members = new List<string>();
        while (!this.Check(CppTokenKind.RightBrace) && !this.IsAtEnd())
        {
            var memberName = this.Expect(CppTokenKind.Identifier);
            var memberText = memberName.Text;

            if (this.Match(CppTokenKind.Assign))
            {
                var val = this.ParseExpression();
                _ = val; // value is tracked in the expression
            }

            members.Add(memberText);

            if (!this.Match(CppTokenKind.Comma))
            {
                break;
            }
        }

        this.Expect(CppTokenKind.RightBrace);
        this.Expect(CppTokenKind.Semicolon);

        // Store enum as a named type declaration — emitter will handle it
        return new CppVariableDeclaration
        {
            Type = new CppNamedType { Name = "enum " + name.Text },
            Name = name.Text,
            Line = start.Line,
            Column = start.Column,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Function or global variable
    // ────────────────────────────────────────────────────────
    private CppDeclarationNode ParseFunctionOrVariable()
    {
        var type = this.ParseType();
        var name = this.Expect(CppTokenKind.Identifier);

        // Check for out-of-class method definition: ReturnType ClassName::method(...)
        string? classScope = null;
        if (this.Match(CppTokenKind.ScopeResolution))
        {
            classScope = name.Text;
            name = this.Expect(CppTokenKind.Identifier);
        }

        // Function declaration
        if (this.Check(CppTokenKind.LeftParen))
        {
            this.Expect(CppTokenKind.LeftParen);
            var parameters = this.ParseParameterList();
            this.Expect(CppTokenKind.RightParen);

            var isConst = this.Match(CppTokenKind.ConstKeyword);

            CppBlockStatement? body = null;
            if (this.Check(CppTokenKind.LeftBrace))
            {
                body = this.ParseBlock();
            }
            else
            {
                this.Expect(CppTokenKind.Semicolon);
            }

            if (classScope is not null)
            {
                return new CppMethodDeclaration
                {
                    ReturnType = type,
                    Name = name.Text,
                    Parameters = parameters,
                    Body = body,
                    IsConst = isConst,
                    Line = name.Line,
                    Column = name.Column,
                };
            }

            return new CppFunctionDeclaration
            {
                ReturnType = type,
                Name = name.Text,
                ClassScope = classScope,
                Parameters = parameters,
                Body = body,
                Line = name.Line,
                Column = name.Column,
            };
        }

        // Array type suffix: int a[10];
        if (this.Check(CppTokenKind.LeftBracket))
        {
            type = this.ParseArraySuffix(type);
        }

        // Global variable
        CppExpressionNode? init = null;
        if (this.Match(CppTokenKind.Assign))
        {
            init = this.ParseExpression();
        }

        this.Expect(CppTokenKind.Semicolon);
        return new CppVariableDeclaration
        {
            Type = type,
            Name = name.Text,
            Initializer = init,
            Line = name.Line,
            Column = name.Column,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Parameters
    // ────────────────────────────────────────────────────────
    private List<CppParameterDeclaration> ParseParameterList()
    {
        var parameters = new List<CppParameterDeclaration>();

        if (this.Check(CppTokenKind.RightParen))
        {
            return parameters;
        }

        // Handle (void)
        if (this.Check(CppTokenKind.VoidKeyword) &&
            this.position + 1 < tokens.Count &&
            tokens[this.position + 1].Kind == CppTokenKind.RightParen)
        {
            this.Advance();
            return parameters;
        }

        do
        {
            var type = this.ParseType();
            var name = this.Expect(CppTokenKind.Identifier);

            // Default parameter value
            CppExpressionNode? defaultValue = null;
            if (this.Match(CppTokenKind.Assign))
            {
                defaultValue = this.ParseAssignment();
            }

            parameters.Add(new CppParameterDeclaration
            {
                Type = type,
                Name = name.Text,
                DefaultValue = defaultValue,
                Line = name.Line,
                Column = name.Column,
            });
        }
        while (this.Match(CppTokenKind.Comma));

        return parameters;
    }

    // ────────────────────────────────────────────────────────
    //  Statements
    // ────────────────────────────────────────────────────────
    private CppBlockStatement ParseBlock()
    {
        var start = this.Expect(CppTokenKind.LeftBrace);
        var statements = new List<CppStatementNode>();

        while (!this.Check(CppTokenKind.RightBrace) && !this.IsAtEnd())
        {
            statements.Add(this.ParseStatement());
        }

        this.Expect(CppTokenKind.RightBrace);
        return new CppBlockStatement { Statements = statements, Line = start.Line, Column = start.Column };
    }

    private CppStatementNode ParseStatement()
    {
        if (this.Check(CppTokenKind.LeftBrace))
        {
            return this.ParseBlock();
        }

        if (this.Check(CppTokenKind.ReturnKeyword))
        {
            return this.ParseReturnStatement();
        }

        if (this.Check(CppTokenKind.IfKeyword))
        {
            return this.ParseIfStatement();
        }

        if (this.Check(CppTokenKind.WhileKeyword))
        {
            return this.ParseWhileStatement();
        }

        if (this.Check(CppTokenKind.ForKeyword))
        {
            return this.ParseForStatement();
        }

        if (this.Check(CppTokenKind.DoKeyword))
        {
            return this.ParseDoWhileStatement();
        }

        if (this.Check(CppTokenKind.SwitchKeyword))
        {
            return this.ParseSwitchStatement();
        }

        if (this.Check(CppTokenKind.BreakKeyword))
        {
            var start = this.Advance();
            this.Expect(CppTokenKind.Semicolon);
            return new CppBreakStatement { Line = start.Line, Column = start.Column };
        }

        if (this.Check(CppTokenKind.ContinueKeyword))
        {
            var start = this.Advance();
            this.Expect(CppTokenKind.Semicolon);
            return new CppContinueStatement { Line = start.Line, Column = start.Column };
        }

        // cout << ...;
        if (this.IsCoutStatement())
        {
            return this.ParseCoutStatement();
        }

        // cin >> ...;
        if (this.IsCinStatement())
        {
            return this.ParseCinStatement();
        }

        // delete expr;
        if (this.Check(CppTokenKind.DeleteKeyword))
        {
            return this.ParseDeleteStatement();
        }

        // Variable declaration: type name ...;
        if (this.IsTypeStart())
        {
            return this.ParseVariableDeclarationStatement();
        }

        // Expression statement
        var expr = this.ParseExpression();
        this.Expect(CppTokenKind.Semicolon);
        return new CppExpressionStatement { Expression = expr, Line = expr.Line, Column = expr.Column };
    }

    private CppReturnStatement ParseReturnStatement()
    {
        var start = this.Expect(CppTokenKind.ReturnKeyword);
        CppExpressionNode? value = null;
        if (!this.Check(CppTokenKind.Semicolon))
        {
            value = this.ParseExpression();
        }

        this.Expect(CppTokenKind.Semicolon);
        return new CppReturnStatement { Value = value, Line = start.Line, Column = start.Column };
    }

    private CppIfStatement ParseIfStatement()
    {
        var start = this.Expect(CppTokenKind.IfKeyword);
        this.Expect(CppTokenKind.LeftParen);
        var condition = this.ParseExpression();
        this.Expect(CppTokenKind.RightParen);
        var then = this.ParseStatement();

        CppStatementNode? elseBranch = null;
        if (this.Match(CppTokenKind.ElseKeyword))
        {
            elseBranch = this.ParseStatement();
        }

        return new CppIfStatement
        {
            Condition = condition,
            Then = then,
            Else = elseBranch,
            Line = start.Line,
            Column = start.Column,
        };
    }

    private CppWhileStatement ParseWhileStatement()
    {
        var start = this.Expect(CppTokenKind.WhileKeyword);
        this.Expect(CppTokenKind.LeftParen);
        var condition = this.ParseExpression();
        this.Expect(CppTokenKind.RightParen);
        var body = this.ParseStatement();
        return new CppWhileStatement
        {
            Condition = condition,
            Body = body,
            Line = start.Line,
            Column = start.Column,
        };
    }

    private CppForStatement ParseForStatement()
    {
        var start = this.Expect(CppTokenKind.ForKeyword);
        this.Expect(CppTokenKind.LeftParen);

        // Init
        CppStatementNode? init = null;
        if (!this.Check(CppTokenKind.Semicolon))
        {
            if (this.IsTypeStart())
            {
                init = this.ParseVariableDeclarationStatement();
            }
            else
            {
                var expr = this.ParseExpression();
                this.Expect(CppTokenKind.Semicolon);
                init = new CppExpressionStatement { Expression = expr, Line = expr.Line, Column = expr.Column };
            }
        }
        else
        {
            this.Advance(); // skip ;
        }

        // Condition
        CppExpressionNode? condition = null;
        if (!this.Check(CppTokenKind.Semicolon))
        {
            condition = this.ParseExpression();
        }

        this.Expect(CppTokenKind.Semicolon);

        // Increment
        CppExpressionNode? increment = null;
        if (!this.Check(CppTokenKind.RightParen))
        {
            increment = this.ParseExpression();
        }

        this.Expect(CppTokenKind.RightParen);
        var body = this.ParseStatement();

        return new CppForStatement
        {
            Init = init,
            Condition = condition,
            Increment = increment,
            Body = body,
            Line = start.Line,
            Column = start.Column,
        };
    }

    private CppDoWhileStatement ParseDoWhileStatement()
    {
        var start = this.Expect(CppTokenKind.DoKeyword);
        var body = this.ParseStatement();
        this.Expect(CppTokenKind.WhileKeyword);
        this.Expect(CppTokenKind.LeftParen);
        var condition = this.ParseExpression();
        this.Expect(CppTokenKind.RightParen);
        this.Expect(CppTokenKind.Semicolon);
        return new CppDoWhileStatement
        {
            Body = body,
            Condition = condition,
            Line = start.Line,
            Column = start.Column,
        };
    }

    private CppSwitchStatement ParseSwitchStatement()
    {
        var start = this.Expect(CppTokenKind.SwitchKeyword);
        this.Expect(CppTokenKind.LeftParen);
        var expr = this.ParseExpression();
        this.Expect(CppTokenKind.RightParen);
        this.Expect(CppTokenKind.LeftBrace);

        var cases = new List<CppCaseClause>();
        while (!this.Check(CppTokenKind.RightBrace) && !this.IsAtEnd())
        {
            CppExpressionNode? caseValue = null;
            int caseLine = this.Peek().Line, caseCol = this.Peek().Column;

            if (this.Match(CppTokenKind.CaseKeyword))
            {
                caseValue = this.ParseExpression();
                this.Expect(CppTokenKind.Colon);
            }
            else if (this.Match(CppTokenKind.DefaultKeyword))
            {
                this.Expect(CppTokenKind.Colon);
            }

            var body = new List<CppStatementNode>();
            while (!this.Check(CppTokenKind.CaseKeyword) &&
                   !this.Check(CppTokenKind.DefaultKeyword) &&
                   !this.Check(CppTokenKind.RightBrace) &&
                   !this.IsAtEnd())
            {
                body.Add(this.ParseStatement());
            }

            cases.Add(new CppCaseClause { Value = caseValue, Body = body, Line = caseLine, Column = caseCol });
        }

        this.Expect(CppTokenKind.RightBrace);
        return new CppSwitchStatement { Expression = expr, Cases = cases, Line = start.Line, Column = start.Column };
    }

    // ────────────────────────────────────────────────────────
    //  cout / cin
    // ────────────────────────────────────────────────────────
    private bool IsCoutStatement()
    {
        return this.Check(CppTokenKind.Identifier) && this.Peek().Text == "cout";
    }

    private bool IsCinStatement()
    {
        return this.Check(CppTokenKind.Identifier) && this.Peek().Text == "cin";
    }

    private CppCoutStatement ParseCoutStatement()
    {
        var start = this.Advance(); // consume 'cout'
        var expressions = new List<CppExpressionNode>();

        while (this.Match(CppTokenKind.ShiftLeft))
        {
            if (this.Check(CppTokenKind.Identifier) && this.Peek().Text == "endl")
            {
                this.Advance();
                expressions.Add(new CppEndlExpression { Line = this.PreviousToken().Line, Column = this.PreviousToken().Column });
            }
            else
            {
                // Parse up to additive level so we don't consume << as shift operator.
                // For relational/logical in cout, users must parenthesise: cout << (a > b)
                expressions.Add(this.ParseCoutElement());
            }
        }

        this.Expect(CppTokenKind.Semicolon);
        return new CppCoutStatement { Expressions = expressions, Line = start.Line, Column = start.Column };
    }

    /// <summary>
    /// Parses a single cout insertion element. Stops before <c>&lt;&lt;</c> so it is
    /// not consumed as a shift operator. Handles ternary with parentheses.
    /// </summary>
    private CppExpressionNode ParseCoutElement()
    {
        // Parenthesised expressions at the top level allow any inner expression
        // so cout << (a > b) still works.
        return this.ParseAdditive();
    }

    private CppCinStatement ParseCinStatement()
    {
        var start = this.Advance(); // consume 'cin'
        var targets = new List<CppExpressionNode>();

        while (this.Match(CppTokenKind.ShiftRight))
        {
            targets.Add(this.ParsePrimary());
        }

        this.Expect(CppTokenKind.Semicolon);
        return new CppCinStatement { Targets = targets, Line = start.Line, Column = start.Column };
    }

    // ────────────────────────────────────────────────────────
    //  delete statement
    // ────────────────────────────────────────────────────────
    private CppExpressionStatement ParseDeleteStatement()
    {
        var start = this.Expect(CppTokenKind.DeleteKeyword);
        var operand = this.ParseUnary();
        this.Expect(CppTokenKind.Semicolon);
        return new CppExpressionStatement
        {
            Expression = new CppDeleteExpression { Operand = operand, Line = start.Line, Column = start.Column },
            Line = start.Line,
            Column = start.Column,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Variable declaration statement
    // ────────────────────────────────────────────────────────
    private CppVariableDeclarationStatement ParseVariableDeclarationStatement()
    {
        var type = this.ParseType();
        var name = this.Expect(CppTokenKind.Identifier);

        // Array suffix
        if (this.Check(CppTokenKind.LeftBracket))
        {
            type = this.ParseArraySuffix(type);
        }

        CppExpressionNode? init = null;
        if (this.Match(CppTokenKind.Assign))
        {
            init = this.ParseExpression();
        }
        else if (this.Check(CppTokenKind.LeftParen))
        {
            // Constructor-style init: Type name(arg1, arg2);
            this.Expect(CppTokenKind.LeftParen);
            var args = new List<CppExpressionNode>();
            if (!this.Check(CppTokenKind.RightParen))
            {
                do
                {
                    args.Add(this.ParseAssignment());
                }
                while (this.Match(CppTokenKind.Comma));
            }

            this.Expect(CppTokenKind.RightParen);

            // Treat as a call expression to the constructor
            init = new CppCallExpression
            {
                Callee = new CppIdentifierExpression { Name = type is CppNamedType nt ? nt.Name : "unknown", Line = name.Line, Column = name.Column },
                Arguments = args,
                Line = name.Line,
                Column = name.Column,
            };
        }

        this.Expect(CppTokenKind.Semicolon);
        return new CppVariableDeclarationStatement
        {
            Declaration = new CppVariableDeclaration
            {
                Type = type,
                Name = name.Text,
                Initializer = init,
                Line = name.Line,
                Column = name.Column,
            },
            Line = name.Line,
            Column = name.Column,
        };
    }

    // ────────────────────────────────────────────────────────
    //  Types
    // ────────────────────────────────────────────────────────
    private bool IsTypeStart()
    {
        var kind = this.Peek().Kind;
        if (kind is CppTokenKind.IntKeyword or CppTokenKind.CharKeyword or CppTokenKind.VoidKeyword
            or CppTokenKind.BoolKeyword or CppTokenKind.ShortKeyword or CppTokenKind.LongKeyword
            or CppTokenKind.StructKeyword or CppTokenKind.EnumKeyword or CppTokenKind.UnionKeyword
            or CppTokenKind.ConstKeyword or CppTokenKind.SignedKeyword or CppTokenKind.UnsignedKeyword
            or CppTokenKind.StaticKeyword)
        {
            return true;
        }

        // Known class name or template parameter used as type
        if (kind == CppTokenKind.Identifier)
        {
            var text = this.Peek().Text;
            if (this.knownClassNames.Contains(text) || this.knownTemplateParams.Contains(text) || text == "string")
            {
                // Look ahead to see if followed by identifier (type name pattern) or * or &
                var la = this.position + 1;
                if (la < tokens.Count)
                {
                    var nextKind = tokens[la].Kind;
                    return nextKind is CppTokenKind.Identifier or CppTokenKind.Asterisk or CppTokenKind.Ampersand;
                }
            }
        }

        return false;
    }

    private CppTypeNode ParseType()
    {
        var isConst = this.Match(CppTokenKind.ConstKeyword);
        var isUnsigned = this.Match(CppTokenKind.UnsignedKeyword);
        var isSigned = this.Match(CppTokenKind.SignedKeyword);

        // Skip static/extern qualifiers
        this.Match(CppTokenKind.StaticKeyword);
        this.Match(CppTokenKind.ExternKeyword);

        CppTypeNode baseType;

        if (this.Check(CppTokenKind.IntKeyword))
        {
            this.Advance();
            baseType = new CppPrimitiveType { Kind = CppPrimitiveKind.Int, IsUnsigned = isUnsigned, IsConst = isConst };
        }
        else if (this.Check(CppTokenKind.CharKeyword))
        {
            this.Advance();
            baseType = new CppPrimitiveType { Kind = CppPrimitiveKind.Char, IsUnsigned = isUnsigned, IsConst = isConst };
        }
        else if (this.Check(CppTokenKind.VoidKeyword))
        {
            this.Advance();
            baseType = new CppPrimitiveType { Kind = CppPrimitiveKind.Void, IsConst = isConst };
        }
        else if (this.Check(CppTokenKind.BoolKeyword))
        {
            this.Advance();
            baseType = new CppPrimitiveType { Kind = CppPrimitiveKind.Bool, IsConst = isConst };
        }
        else if (this.Check(CppTokenKind.ShortKeyword))
        {
            this.Advance();
            this.Match(CppTokenKind.IntKeyword); // optional "int" after "short"
            baseType = new CppPrimitiveType { Kind = CppPrimitiveKind.Short, IsUnsigned = isUnsigned, IsConst = isConst };
        }
        else if (this.Check(CppTokenKind.LongKeyword))
        {
            this.Advance();
            this.Match(CppTokenKind.IntKeyword); // optional "int" after "long"
            baseType = new CppPrimitiveType { Kind = CppPrimitiveKind.Long, IsUnsigned = isUnsigned, IsConst = isConst };
        }
        else if (this.Check(CppTokenKind.StructKeyword) || this.Check(CppTokenKind.EnumKeyword) || this.Check(CppTokenKind.UnionKeyword))
        {
            this.Advance();
            var name = this.Expect(CppTokenKind.Identifier);
            baseType = new CppNamedType { Name = name.Text, IsConst = isConst };
        }
        else if (isUnsigned || isSigned)
        {
            // "unsigned" or "signed" alone → int
            baseType = new CppPrimitiveType { Kind = CppPrimitiveKind.Int, IsUnsigned = isUnsigned, IsConst = isConst };
        }
        else
        {
            // Named type (class name, template param, string)
            var name = this.Expect(CppTokenKind.Identifier);

            // Template instantiation: Type<T>
            var typeArgs = new List<CppTypeNode>();
            if (this.Match(CppTokenKind.Less))
            {
                do
                {
                    typeArgs.Add(this.ParseType());
                }
                while (this.Match(CppTokenKind.Comma));

                this.Expect(CppTokenKind.Greater);
            }

            baseType = new CppNamedType { Name = name.Text, TypeArguments = typeArgs, IsConst = isConst };
        }

        // Pointer/reference suffix
        while (this.Check(CppTokenKind.Asterisk) || this.Check(CppTokenKind.Ampersand))
        {
            if (this.Match(CppTokenKind.Asterisk))
            {
                var ptrConst = this.Match(CppTokenKind.ConstKeyword);
                baseType = new CppPointerType { Inner = baseType, IsConst = ptrConst };
            }
            else if (this.Match(CppTokenKind.Ampersand))
            {
                var refConst = this.Match(CppTokenKind.ConstKeyword);
                baseType = new CppReferenceType { Inner = baseType, IsConst = refConst };
            }
        }

        return baseType;
    }

    private CppArrayType ParseArraySuffix(CppTypeNode elementType)
    {
        this.Expect(CppTokenKind.LeftBracket);
        CppExpressionNode? size = null;
        if (!this.Check(CppTokenKind.RightBracket))
        {
            size = this.ParseExpression();
        }

        this.Expect(CppTokenKind.RightBracket);
        return new CppArrayType { ElementType = elementType, Size = size };
    }

    // ────────────────────────────────────────────────────────
    //  Expressions (operator precedence climbing)
    // ────────────────────────────────────────────────────────
    private CppExpressionNode ParseExpression()
    {
        return this.ParseAssignment();
    }

    private CppExpressionNode ParseAssignment()
    {
        var left = this.ParseTernary();

        if (this.Match(CppTokenKind.Assign))
        {
            var right = this.ParseAssignment();
            return new CppBinaryExpression
            {
                Left = left,
                Operator = CppBinaryOperator.Assign,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        // Compound assignment
        var compoundOp = this.TryParseCompoundAssignment();
        if (compoundOp.HasValue)
        {
            var right = this.ParseAssignment();
            return new CppCompoundAssignmentExpression
            {
                Left = left,
                Operator = compoundOp.Value,
                Right = right,
                Line = left.Line,
                Column = left.Column,
            };
        }

        return left;
    }

    private CppBinaryOperator? TryParseCompoundAssignment()
    {
        var kind = this.Peek().Kind;
        CppBinaryOperator? op = kind switch
        {
            CppTokenKind.PlusEqual => CppBinaryOperator.Add,
            CppTokenKind.MinusEqual => CppBinaryOperator.Sub,
            CppTokenKind.StarEqual => CppBinaryOperator.Mul,
            CppTokenKind.SlashEqual => CppBinaryOperator.Div,
            CppTokenKind.PercentEqual => CppBinaryOperator.Mod,
            CppTokenKind.AmpEqual => CppBinaryOperator.BitwiseAnd,
            CppTokenKind.PipeEqual => CppBinaryOperator.BitwiseOr,
            CppTokenKind.CaretEqual => CppBinaryOperator.BitwiseXor,
            CppTokenKind.ShiftLeftEqual => CppBinaryOperator.Shl,
            CppTokenKind.ShiftRightEqual => CppBinaryOperator.Shr,
            _ => null,
        };

        if (op.HasValue)
        {
            this.Advance();
        }

        return op;
    }

    private CppExpressionNode ParseTernary()
    {
        var expr = this.ParseLogicalOr();

        if (this.Match(CppTokenKind.Question))
        {
            var trueExpr = this.ParseExpression();
            this.Expect(CppTokenKind.Colon);
            var falseExpr = this.ParseTernary();
            return new CppTernaryExpression
            {
                Condition = expr,
                TrueExpr = trueExpr,
                FalseExpr = falseExpr,
                Line = expr.Line,
                Column = expr.Column,
            };
        }

        return expr;
    }

    private CppExpressionNode ParseLogicalOr()
    {
        var left = this.ParseLogicalAnd();
        while (this.Match(CppTokenKind.LogicalOr))
        {
            var right = this.ParseLogicalAnd();
            left = new CppBinaryExpression { Left = left, Operator = CppBinaryOperator.Or, Right = right, Line = left.Line, Column = left.Column };
        }

        return left;
    }

    private CppExpressionNode ParseLogicalAnd()
    {
        var left = this.ParseBitwiseOr();
        while (this.Match(CppTokenKind.LogicalAnd))
        {
            var right = this.ParseBitwiseOr();
            left = new CppBinaryExpression { Left = left, Operator = CppBinaryOperator.And, Right = right, Line = left.Line, Column = left.Column };
        }

        return left;
    }

    private CppExpressionNode ParseBitwiseOr()
    {
        var left = this.ParseBitwiseXor();
        while (this.Match(CppTokenKind.Pipe))
        {
            var right = this.ParseBitwiseXor();
            left = new CppBinaryExpression { Left = left, Operator = CppBinaryOperator.BitwiseOr, Right = right, Line = left.Line, Column = left.Column };
        }

        return left;
    }

    private CppExpressionNode ParseBitwiseXor()
    {
        var left = this.ParseBitwiseAnd();
        while (this.Match(CppTokenKind.Caret))
        {
            var right = this.ParseBitwiseAnd();
            left = new CppBinaryExpression { Left = left, Operator = CppBinaryOperator.BitwiseXor, Right = right, Line = left.Line, Column = left.Column };
        }

        return left;
    }

    private CppExpressionNode ParseBitwiseAnd()
    {
        var left = this.ParseEquality();
        while (this.Check(CppTokenKind.Ampersand) && !IsReferenceType())
        {
            this.Advance();
            var right = this.ParseEquality();
            left = new CppBinaryExpression { Left = left, Operator = CppBinaryOperator.BitwiseAnd, Right = right, Line = left.Line, Column = left.Column };
        }

        return left;
    }

    private CppExpressionNode ParseEquality()
    {
        var left = this.ParseRelational();
        while (this.Check(CppTokenKind.Equal) || this.Check(CppTokenKind.NotEqual))
        {
            var op = this.Advance().Kind == CppTokenKind.Equal ? CppBinaryOperator.Equal : CppBinaryOperator.NotEqual;
            var right = this.ParseRelational();
            left = new CppBinaryExpression { Left = left, Operator = op, Right = right, Line = left.Line, Column = left.Column };
        }

        return left;
    }

    private CppExpressionNode ParseRelational()
    {
        var left = this.ParseShift();
        while (this.Check(CppTokenKind.Less) || this.Check(CppTokenKind.LessEqual) ||
               this.Check(CppTokenKind.Greater) || this.Check(CppTokenKind.GreaterEqual))
        {
            var tok = this.Advance();
            var op = tok.Kind switch
            {
                CppTokenKind.Less => CppBinaryOperator.Less,
                CppTokenKind.LessEqual => CppBinaryOperator.LessEqual,
                CppTokenKind.Greater => CppBinaryOperator.Greater,
                CppTokenKind.GreaterEqual => CppBinaryOperator.GreaterEqual,
                _ => throw new CompilerInvariantException("Unexpected relational operator"),
            };
            var right = this.ParseShift();
            left = new CppBinaryExpression { Left = left, Operator = op, Right = right, Line = left.Line, Column = left.Column };
        }

        return left;
    }

    private CppExpressionNode ParseShift()
    {
        var left = this.ParseAdditive();
        while (this.Check(CppTokenKind.ShiftLeft) || this.Check(CppTokenKind.ShiftRight))
        {
            var tok = this.Advance();
            var op = tok.Kind == CppTokenKind.ShiftLeft ? CppBinaryOperator.Shl : CppBinaryOperator.Shr;
            var right = this.ParseAdditive();
            left = new CppBinaryExpression { Left = left, Operator = op, Right = right, Line = left.Line, Column = left.Column };
        }

        return left;
    }

    private CppExpressionNode ParseAdditive()
    {
        var left = this.ParseMultiplicative();
        while (this.Check(CppTokenKind.Plus) || this.Check(CppTokenKind.Minus))
        {
            var tok = this.Advance();
            var op = tok.Kind == CppTokenKind.Plus ? CppBinaryOperator.Add : CppBinaryOperator.Sub;
            var right = this.ParseMultiplicative();
            left = new CppBinaryExpression { Left = left, Operator = op, Right = right, Line = left.Line, Column = left.Column };
        }

        return left;
    }

    private CppExpressionNode ParseMultiplicative()
    {
        var left = this.ParseUnary();
        while (this.Check(CppTokenKind.Asterisk) || this.Check(CppTokenKind.Slash) || this.Check(CppTokenKind.Percent))
        {
            var tok = this.Advance();
            var op = tok.Kind switch
            {
                CppTokenKind.Asterisk => CppBinaryOperator.Mul,
                CppTokenKind.Slash => CppBinaryOperator.Div,
                CppTokenKind.Percent => CppBinaryOperator.Mod,
                _ => throw new CompilerInvariantException("Unexpected multiplicative operator"),
            };
            var right = this.ParseUnary();
            left = new CppBinaryExpression { Left = left, Operator = op, Right = right, Line = left.Line, Column = left.Column };
        }

        return left;
    }

    private CppExpressionNode ParseUnary()
    {
        if (this.Match(CppTokenKind.Minus))
        {
            var operand = this.ParseUnary();
            return new CppUnaryExpression { Operator = CppUnaryOperator.Negate, Operand = operand, Line = operand.Line, Column = operand.Column };
        }

        if (this.Match(CppTokenKind.Exclamation))
        {
            var operand = this.ParseUnary();
            return new CppUnaryExpression { Operator = CppUnaryOperator.LogicalNot, Operand = operand, Line = operand.Line, Column = operand.Column };
        }

        if (this.Match(CppTokenKind.Tilde))
        {
            var operand = this.ParseUnary();
            return new CppUnaryExpression { Operator = CppUnaryOperator.BitwiseNot, Operand = operand, Line = operand.Line, Column = operand.Column };
        }

        if (this.Match(CppTokenKind.Asterisk))
        {
            var operand = this.ParseUnary();
            return new CppUnaryExpression { Operator = CppUnaryOperator.Dereference, Operand = operand, Line = operand.Line, Column = operand.Column };
        }

        if (this.Match(CppTokenKind.Ampersand))
        {
            var operand = this.ParseUnary();
            return new CppUnaryExpression { Operator = CppUnaryOperator.AddressOf, Operand = operand, Line = operand.Line, Column = operand.Column };
        }

        if (this.Match(CppTokenKind.PlusPlus))
        {
            var operand = this.ParseUnary();
            return new CppUnaryExpression { Operator = CppUnaryOperator.PreIncrement, Operand = operand, Line = operand.Line, Column = operand.Column };
        }

        if (this.Match(CppTokenKind.MinusMinus))
        {
            var operand = this.ParseUnary();
            return new CppUnaryExpression { Operator = CppUnaryOperator.PreDecrement, Operand = operand, Line = operand.Line, Column = operand.Column };
        }

        // new Type(args)
        if (this.Check(CppTokenKind.NewKeyword))
        {
            return this.ParseNewExpression();
        }

        // sizeof
        if (this.Check(CppTokenKind.SizeofKeyword))
        {
            return this.ParseSizeofExpression();
        }

        // C-style cast: (type)expr — only if type keyword follows
        if (this.Check(CppTokenKind.LeftParen) && this.IsCastExpression())
        {
            return this.ParseCastExpression();
        }

        return this.ParsePostfix();
    }

    private CppNewExpression ParseNewExpression()
    {
        var start = this.Expect(CppTokenKind.NewKeyword);
        var type = this.ParseType();

        var args = new List<CppExpressionNode>();
        if (this.Match(CppTokenKind.LeftParen))
        {
            if (!this.Check(CppTokenKind.RightParen))
            {
                do
                {
                    args.Add(this.ParseAssignment());
                }
                while (this.Match(CppTokenKind.Comma));
            }

            this.Expect(CppTokenKind.RightParen);
        }

        return new CppNewExpression { Type = type, Arguments = args, Line = start.Line, Column = start.Column };
    }

    private CppSizeofExpression ParseSizeofExpression()
    {
        var start = this.Expect(CppTokenKind.SizeofKeyword);
        this.Expect(CppTokenKind.LeftParen);

        if (this.IsTypeStart())
        {
            var type = this.ParseType();
            this.Expect(CppTokenKind.RightParen);
            return new CppSizeofExpression { Type = type, Line = start.Line, Column = start.Column };
        }

        var operand = this.ParseExpression();
        this.Expect(CppTokenKind.RightParen);
        return new CppSizeofExpression { Operand = operand, Line = start.Line, Column = start.Column };
    }

    private bool IsCastExpression()
    {
        // Lookahead: ( <type-keyword> ) <expr>
        if (this.position + 2 >= tokens.Count)
        {
            return false;
        }

        var nextKind = tokens[this.position + 1].Kind;
        return nextKind is CppTokenKind.IntKeyword or CppTokenKind.CharKeyword or CppTokenKind.VoidKeyword
            or CppTokenKind.BoolKeyword or CppTokenKind.ShortKeyword or CppTokenKind.LongKeyword;
    }

    private CppCastExpression ParseCastExpression()
    {
        var start = this.Expect(CppTokenKind.LeftParen);
        var type = this.ParseType();
        this.Expect(CppTokenKind.RightParen);
        var operand = this.ParseUnary();
        return new CppCastExpression { Type = type, Operand = operand, Line = start.Line, Column = start.Column };
    }

    private CppExpressionNode ParsePostfix()
    {
        var expr = this.ParsePrimary();

        while (true)
        {
            if (this.Match(CppTokenKind.LeftParen))
            {
                // Function call
                var args = new List<CppExpressionNode>();
                if (!this.Check(CppTokenKind.RightParen))
                {
                    do
                    {
                        args.Add(this.ParseAssignment());
                    }
                    while (this.Match(CppTokenKind.Comma));
                }

                this.Expect(CppTokenKind.RightParen);
                expr = new CppCallExpression { Callee = expr, Arguments = args, Line = expr.Line, Column = expr.Column };
            }
            else if (this.Match(CppTokenKind.LeftBracket))
            {
                // Array access
                var index = this.ParseExpression();
                this.Expect(CppTokenKind.RightBracket);
                expr = new CppArrayAccessExpression { Array = expr, Index = index, Line = expr.Line, Column = expr.Column };
            }
            else if (this.Match(CppTokenKind.Dot))
            {
                var member = this.Expect(CppTokenKind.Identifier);
                expr = new CppMemberAccessExpression { Object = expr, Member = member.Text, IsArrow = false, Line = expr.Line, Column = expr.Column };
            }
            else if (this.Match(CppTokenKind.Arrow))
            {
                var member = this.Expect(CppTokenKind.Identifier);
                expr = new CppMemberAccessExpression { Object = expr, Member = member.Text, IsArrow = true, Line = expr.Line, Column = expr.Column };
            }
            else if (this.Match(CppTokenKind.PlusPlus))
            {
                expr = new CppPostfixExpression { Operand = expr, Operator = CppPostfixOperator.Increment, Line = expr.Line, Column = expr.Column };
            }
            else if (this.Match(CppTokenKind.MinusMinus))
            {
                expr = new CppPostfixExpression { Operand = expr, Operator = CppPostfixOperator.Decrement, Line = expr.Line, Column = expr.Column };
            }
            else if (this.Match(CppTokenKind.ScopeResolution))
            {
                var member = this.Expect(CppTokenKind.Identifier);
                if (expr is CppIdentifierExpression id)
                {
                    expr = new CppScopeResolutionExpression { Scope = id.Name, Member = member.Text, Line = expr.Line, Column = expr.Column };
                }
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private CppExpressionNode ParsePrimary()
    {
        // Integer literal
        if (this.Check(CppTokenKind.IntegerLiteral))
        {
            var tok = this.Advance();
            return new CppIntegerLiteral { Value = tok.IntValue, Line = tok.Line, Column = tok.Column };
        }

        // Char literal
        if (this.Check(CppTokenKind.CharLiteral))
        {
            var tok = this.Advance();
            return new CppCharLiteral { Value = tok.CharValue, Line = tok.Line, Column = tok.Column };
        }

        // String literal
        if (this.Check(CppTokenKind.StringLiteral))
        {
            var tok = this.Advance();
            return new CppStringLiteral { Value = tok.StringValue ?? string.Empty, Line = tok.Line, Column = tok.Column };
        }

        // Boolean literals
        if (this.Check(CppTokenKind.TrueKeyword))
        {
            var tok = this.Advance();
            return new CppBoolLiteral { Value = true, Line = tok.Line, Column = tok.Column };
        }

        if (this.Check(CppTokenKind.FalseKeyword))
        {
            var tok = this.Advance();
            return new CppBoolLiteral { Value = false, Line = tok.Line, Column = tok.Column };
        }

        // nullptr
        if (this.Check(CppTokenKind.NullptrKeyword))
        {
            var tok = this.Advance();
            return new CppNullptrLiteral { Line = tok.Line, Column = tok.Column };
        }

        // this
        if (this.Check(CppTokenKind.ThisKeyword))
        {
            var tok = this.Advance();
            return new CppThisExpression { Line = tok.Line, Column = tok.Column };
        }

        // Parenthesised expression
        if (this.Match(CppTokenKind.LeftParen))
        {
            var expr = this.ParseExpression();
            this.Expect(CppTokenKind.RightParen);
            return expr;
        }

        // Identifier
        if (this.Check(CppTokenKind.Identifier))
        {
            var tok = this.Advance();
            return new CppIdentifierExpression { Name = tok.Text, Line = tok.Line, Column = tok.Column };
        }

        throw new CompilerInvariantException(
            $"Unexpected token '{this.Peek().Text}' ({this.Peek().Kind}) at line {this.Peek().Line}, column {this.Peek().Column}.");
    }

    // ────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────
    private CppAccessModifier? TryParseAccessModifier()
    {
        if (this.Match(CppTokenKind.PublicKeyword))
        {
            return CppAccessModifier.Public;
        }

        if (this.Match(CppTokenKind.PrivateKeyword))
        {
            return CppAccessModifier.Private;
        }

        if (this.Match(CppTokenKind.ProtectedKeyword))
        {
            return CppAccessModifier.Protected;
        }

        return null;
    }

    private CppAccessModifier? TryParseAccessLabel()
    {
        if ((this.Check(CppTokenKind.PublicKeyword) || this.Check(CppTokenKind.PrivateKeyword) || this.Check(CppTokenKind.ProtectedKeyword))
            && this.position + 1 < tokens.Count && tokens[this.position + 1].Kind == CppTokenKind.Colon)
        {
            var mod = this.TryParseAccessModifier();
            this.Expect(CppTokenKind.Colon);
            return mod;
        }

        return null;
    }

    private CppToken Peek() => tokens[this.position];

    private CppToken PreviousToken() => tokens[this.position - 1];

    private bool Check(CppTokenKind kind) => !this.IsAtEnd() && tokens[this.position].Kind == kind;

    private bool Match(CppTokenKind kind)
    {
        if (!this.Check(kind))
        {
            return false;
        }

        this.position++;
        return true;
    }

    private CppToken Advance()
    {
        var tok = tokens[this.position];
        this.position++;
        return tok;
    }

    private CppToken Expect(CppTokenKind kind)
    {
        if (this.IsAtEnd() || tokens[this.position].Kind != kind)
        {
            var current = this.IsAtEnd() ? "end of file" : $"'{tokens[this.position].Text}'";
            throw new CompilerInvariantException(
                $"Expected {kind} but found {current} at line {this.Peek().Line}, column {this.Peek().Column}.");
        }

        return this.Advance();
    }

    private bool IsAtEnd() => this.position >= tokens.Count || tokens[this.position].Kind == CppTokenKind.EndOfFile;
}
