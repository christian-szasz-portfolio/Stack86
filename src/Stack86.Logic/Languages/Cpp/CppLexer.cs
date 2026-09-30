namespace Stack86.Logic.Languages.Cpp;

using Stack86.Common.Exceptions;

/// <summary>
/// Tokenises C++ source code into a flat list of <see cref="CppToken"/> instances.
/// Extends the C lexer pattern with C++ keywords and the <c>::</c> scope resolution operator.
/// </summary>
public sealed class CppLexer(string source)
{
    private static readonly Dictionary<string, CppTokenKind> Keywords = new()
    {
        // C keywords
        ["int"] = CppTokenKind.IntKeyword,
        ["char"] = CppTokenKind.CharKeyword,
        ["void"] = CppTokenKind.VoidKeyword,
        ["return"] = CppTokenKind.ReturnKeyword,
        ["if"] = CppTokenKind.IfKeyword,
        ["else"] = CppTokenKind.ElseKeyword,
        ["while"] = CppTokenKind.WhileKeyword,
        ["for"] = CppTokenKind.ForKeyword,
        ["break"] = CppTokenKind.BreakKeyword,
        ["continue"] = CppTokenKind.ContinueKeyword,
        ["struct"] = CppTokenKind.StructKeyword,
        ["enum"] = CppTokenKind.EnumKeyword,
        ["union"] = CppTokenKind.UnionKeyword,
        ["typedef"] = CppTokenKind.TypedefKeyword,
        ["do"] = CppTokenKind.DoKeyword,
        ["switch"] = CppTokenKind.SwitchKeyword,
        ["case"] = CppTokenKind.CaseKeyword,
        ["default"] = CppTokenKind.DefaultKeyword,
        ["sizeof"] = CppTokenKind.SizeofKeyword,
        ["const"] = CppTokenKind.ConstKeyword,
        ["static"] = CppTokenKind.StaticKeyword,
        ["extern"] = CppTokenKind.ExternKeyword,
        ["signed"] = CppTokenKind.SignedKeyword,
        ["unsigned"] = CppTokenKind.UnsignedKeyword,
        ["long"] = CppTokenKind.LongKeyword,
        ["short"] = CppTokenKind.ShortKeyword,

        // C++ keywords
        ["class"] = CppTokenKind.ClassKeyword,
        ["public"] = CppTokenKind.PublicKeyword,
        ["private"] = CppTokenKind.PrivateKeyword,
        ["protected"] = CppTokenKind.ProtectedKeyword,
        ["namespace"] = CppTokenKind.NamespaceKeyword,
        ["using"] = CppTokenKind.UsingKeyword,
        ["template"] = CppTokenKind.TemplateKeyword,
        ["typename"] = CppTokenKind.TypenameKeyword,
        ["new"] = CppTokenKind.NewKeyword,
        ["delete"] = CppTokenKind.DeleteKeyword,
        ["bool"] = CppTokenKind.BoolKeyword,
        ["true"] = CppTokenKind.TrueKeyword,
        ["false"] = CppTokenKind.FalseKeyword,
        ["virtual"] = CppTokenKind.VirtualKeyword,
        ["override"] = CppTokenKind.OverrideKeyword,
        ["this"] = CppTokenKind.ThisKeyword,
        ["nullptr"] = CppTokenKind.NullptrKeyword,
        ["operator"] = CppTokenKind.OperatorKeyword,
        ["friend"] = CppTokenKind.FriendKeyword,
        ["inline"] = CppTokenKind.InlineKeyword,
    };

    private int position;
    private int line = 1;
    private int column = 1;

    /// <summary>
    /// Tokenises the source into a list of tokens.
    /// </summary>
    public List<CppToken> Tokenize()
    {
        var tokens = new List<CppToken>();

        while (true)
        {
            var token = this.NextToken();
            tokens.Add(token);

            if (token.Kind == CppTokenKind.EndOfFile)
            {
                break;
            }
        }

        return tokens;
    }

    private static bool IsHexDigit(char c)
    {
        return char.IsDigit(c)
            || (c >= 'a' && c <= 'f')
            || (c >= 'A' && c <= 'F');
    }

    private CppToken NextToken()
    {
        this.SkipWhitespaceAndComments();

        if (this.position >= source.Length)
        {
            return this.MakeToken(CppTokenKind.EndOfFile, string.Empty);
        }

        var ch = source[this.position];

        if (char.IsDigit(ch))
        {
            return this.ReadIntegerLiteral();
        }

        if (char.IsLetter(ch) || ch == '_')
        {
            return this.ReadIdentifierOrKeyword();
        }

        if (ch == '\'')
        {
            return this.ReadCharLiteral();
        }

        if (ch == '"')
        {
            return this.ReadStringLiteral();
        }

        // Three-character operators
        if (this.position + 2 < source.Length)
        {
            var threeChar = source.Substring(this.position, 3);
            var threeCharKind = threeChar switch
            {
                "<<=" => CppTokenKind.ShiftLeftEqual,
                ">>=" => CppTokenKind.ShiftRightEqual,
                _ => (CppTokenKind?)null,
            };

            if (threeCharKind.HasValue)
            {
                var token = this.MakeToken(threeCharKind.Value, threeChar);
                this.Advance();
                this.Advance();
                this.Advance();
                return token;
            }
        }

        // Two-character operators (including :: for C++)
        if (this.position + 1 < source.Length)
        {
            var twoChar = source.Substring(this.position, 2);
            var twoCharKind = twoChar switch
            {
                "::" => CppTokenKind.ScopeResolution,
                "==" => CppTokenKind.Equal,
                "!=" => CppTokenKind.NotEqual,
                "<=" => CppTokenKind.LessEqual,
                ">=" => CppTokenKind.GreaterEqual,
                "<<" => CppTokenKind.ShiftLeft,
                ">>" => CppTokenKind.ShiftRight,
                "&&" => CppTokenKind.LogicalAnd,
                "||" => CppTokenKind.LogicalOr,
                "->" => CppTokenKind.Arrow,
                "++" => CppTokenKind.PlusPlus,
                "--" => CppTokenKind.MinusMinus,
                "+=" => CppTokenKind.PlusEqual,
                "-=" => CppTokenKind.MinusEqual,
                "*=" => CppTokenKind.StarEqual,
                "/=" => CppTokenKind.SlashEqual,
                "%=" => CppTokenKind.PercentEqual,
                "&=" => CppTokenKind.AmpEqual,
                "|=" => CppTokenKind.PipeEqual,
                "^=" => CppTokenKind.CaretEqual,
                _ => (CppTokenKind?)null,
            };

            if (twoCharKind.HasValue)
            {
                var token = this.MakeToken(twoCharKind.Value, twoChar);
                this.Advance();
                this.Advance();
                return token;
            }
        }

        // Single-character operators
        var singleKind = ch switch
        {
            '(' => CppTokenKind.LeftParen,
            ')' => CppTokenKind.RightParen,
            '{' => CppTokenKind.LeftBrace,
            '}' => CppTokenKind.RightBrace,
            '[' => CppTokenKind.LeftBracket,
            ']' => CppTokenKind.RightBracket,
            ';' => CppTokenKind.Semicolon,
            ',' => CppTokenKind.Comma,
            '.' => CppTokenKind.Dot,
            '+' => CppTokenKind.Plus,
            '-' => CppTokenKind.Minus,
            '*' => CppTokenKind.Asterisk,
            '/' => CppTokenKind.Slash,
            '%' => CppTokenKind.Percent,
            '&' => CppTokenKind.Ampersand,
            '|' => CppTokenKind.Pipe,
            '^' => CppTokenKind.Caret,
            '~' => CppTokenKind.Tilde,
            '!' => CppTokenKind.Exclamation,
            '<' => CppTokenKind.Less,
            '>' => CppTokenKind.Greater,
            '=' => CppTokenKind.Assign,
            '?' => CppTokenKind.Question,
            ':' => CppTokenKind.Colon,
            _ => (CppTokenKind?)null,
        };

        if (singleKind.HasValue)
        {
            var token = this.MakeToken(singleKind.Value, ch.ToString());
            this.Advance();
            return token;
        }

        throw new UnsupportedSyntaxException(
            $"Unexpected character '{ch}' at line {this.line}, column {this.column}.");
    }

    private CppToken ReadIntegerLiteral()
    {
        var startLine = this.line;
        var startCol = this.column;
        var start = this.position;

        // Hex literal: 0x or 0X
        if (this.position + 1 < source.Length
            && source[this.position] == '0'
            && (source[this.position + 1] == 'x' || source[this.position + 1] == 'X'))
        {
            this.Advance(); // '0'
            this.Advance(); // 'x'

            while (this.position < source.Length && IsHexDigit(source[this.position]))
            {
                this.Advance();
            }

            var hexText = source[start..this.position];
            return new CppToken
            {
                Kind = CppTokenKind.IntegerLiteral,
                Text = hexText,
                Line = startLine,
                Column = startCol,
                IntValue = Convert.ToInt32(hexText[2..], 16),
            };
        }

        while (this.position < source.Length && char.IsDigit(source[this.position]))
        {
            this.Advance();
        }

        var text = source[start..this.position];
        return new CppToken
        {
            Kind = CppTokenKind.IntegerLiteral,
            Text = text,
            Line = startLine,
            Column = startCol,
            IntValue = int.Parse(text),
        };
    }

    private CppToken ReadIdentifierOrKeyword()
    {
        var startLine = this.line;
        var startCol = this.column;
        var start = this.position;

        while (this.position < source.Length &&
               (char.IsLetterOrDigit(source[this.position]) || source[this.position] == '_'))
        {
            this.Advance();
        }

        var text = source[start..this.position];
        var kind = Keywords.TryGetValue(text, out var kw) ? kw : CppTokenKind.Identifier;

        return new CppToken
        {
            Kind = kind,
            Text = text,
            Line = startLine,
            Column = startCol,
        };
    }

    private CppToken ReadCharLiteral()
    {
        var startLine = this.line;
        var startCol = this.column;
        this.Advance(); // skip opening '

        var ch = this.ReadEscapedChar();

        if (this.position >= source.Length || source[this.position] != '\'')
        {
            throw new UnsupportedSyntaxException(
                $"Unterminated character literal at line {startLine}, column {startCol}.");
        }

        this.Advance(); // skip closing '

        return new CppToken
        {
            Kind = CppTokenKind.CharLiteral,
            Text = $"'{ch}'",
            Line = startLine,
            Column = startCol,
            CharValue = ch,
        };
    }

    private CppToken ReadStringLiteral()
    {
        var startLine = this.line;
        var startCol = this.column;
        this.Advance(); // skip opening "

        var sb = new System.Text.StringBuilder();

        while (this.position < source.Length && source[this.position] != '"')
        {
            sb.Append(this.ReadEscapedChar());
        }

        if (this.position >= source.Length)
        {
            throw new UnsupportedSyntaxException(
                $"Unterminated string literal at line {startLine}, column {startCol}.");
        }

        this.Advance(); // skip closing "

        var value = sb.ToString();
        return new CppToken
        {
            Kind = CppTokenKind.StringLiteral,
            Text = $"\"{value}\"",
            Line = startLine,
            Column = startCol,
            StringValue = value,
        };
    }

    private char ReadEscapedChar()
    {
        if (source[this.position] == '\\')
        {
            this.Advance();
            if (this.position >= source.Length)
            {
                throw new UnsupportedSyntaxException("Unexpected end of escape sequence.");
            }

            var escaped = source[this.position];
            this.Advance();

            return escaped switch
            {
                'n' => '\n',
                't' => '\t',
                'r' => '\r',
                '0' => '\0',
                '\\' => '\\',
                '\'' => '\'',
                '"' => '"',
                _ => throw new UnsupportedSyntaxException($"Unknown escape sequence: \\{escaped}"),
            };
        }

        var ch = source[this.position];
        this.Advance();
        return ch;
    }

    private void SkipWhitespaceAndComments()
    {
        while (this.position < source.Length)
        {
            var ch = source[this.position];

            if (char.IsWhiteSpace(ch))
            {
                this.Advance();
                continue;
            }

            // Line comment
            if (ch == '/' && this.position + 1 < source.Length && source[this.position + 1] == '/')
            {
                while (this.position < source.Length && source[this.position] != '\n')
                {
                    this.Advance();
                }

                continue;
            }

            // Preprocessor directive — skip entire line (handled by include mapper)
            if (ch == '#')
            {
                while (this.position < source.Length && source[this.position] != '\n')
                {
                    this.Advance();
                }

                continue;
            }

            // Block comment
            if (ch == '/' && this.position + 1 < source.Length && source[this.position + 1] == '*')
            {
                this.Advance();
                this.Advance();

                while (this.position + 1 < source.Length &&
                       !(source[this.position] == '*' && source[this.position + 1] == '/'))
                {
                    this.Advance();
                }

                if (this.position + 1 < source.Length)
                {
                    this.Advance(); // *
                    this.Advance(); // /
                }

                continue;
            }

            break;
        }
    }

    private CppToken MakeToken(CppTokenKind kind, string text)
    {
        return new CppToken
        {
            Kind = kind,
            Text = text,
            Line = this.line,
            Column = this.column,
        };
    }

    private void Advance()
    {
        if (this.position < source.Length)
        {
            if (source[this.position] == '\n')
            {
                this.line++;
                this.column = 1;
            }
            else
            {
                this.column++;
            }

            this.position++;
        }
    }
}
