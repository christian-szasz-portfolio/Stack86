namespace Stack86.Logic.Languages.C;

using Stack86.Common.Exceptions;

/// <summary>
/// Tokenises C source code into a flat list of <see cref="CToken"/> instances.
/// </summary>
public sealed class CLexer(string source)
{
    private static readonly Dictionary<string, CTokenKind> Keywords = new()
    {
        ["int"] = CTokenKind.IntKeyword,
        ["char"] = CTokenKind.CharKeyword,
        ["void"] = CTokenKind.VoidKeyword,
        ["return"] = CTokenKind.ReturnKeyword,
        ["if"] = CTokenKind.IfKeyword,
        ["else"] = CTokenKind.ElseKeyword,
        ["while"] = CTokenKind.WhileKeyword,
        ["for"] = CTokenKind.ForKeyword,
        ["break"] = CTokenKind.BreakKeyword,
        ["continue"] = CTokenKind.ContinueKeyword,
        ["struct"] = CTokenKind.StructKeyword,
        ["enum"] = CTokenKind.EnumKeyword,
        ["union"] = CTokenKind.UnionKeyword,
        ["typedef"] = CTokenKind.TypedefKeyword,
        ["do"] = CTokenKind.DoKeyword,
        ["switch"] = CTokenKind.SwitchKeyword,
        ["case"] = CTokenKind.CaseKeyword,
        ["default"] = CTokenKind.DefaultKeyword,
        ["sizeof"] = CTokenKind.SizeofKeyword,
    };

    private int position;
    private int line = 1;
    private int column = 1;

    /// <summary>
    /// Tokenises the source into a list of tokens.
    /// </summary>
    public List<CToken> Tokenize()
    {
        var tokens = new List<CToken>();

        while (true)
        {
            var token = this.NextToken();
            tokens.Add(token);

            if (token.Kind == CTokenKind.EndOfFile)
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

    private CToken NextToken()
    {
        this.SkipWhitespaceAndComments();

        if (this.position >= source.Length)
        {
            return this.MakeToken(CTokenKind.EndOfFile, string.Empty);
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

        if (this.position + 2 < source.Length)
        {
            var threeChar = source.Substring(this.position, 3);
            var threeCharKind = threeChar switch
            {
                "<<=" => CTokenKind.ShiftLeftEqual,
                ">>=" => CTokenKind.ShiftRightEqual,
                _ => (CTokenKind?)null,
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

        if (this.position + 1 < source.Length)
        {
            var twoChar = source.Substring(this.position, 2);
            var twoCharKind = twoChar switch
            {
                "==" => CTokenKind.Equal,
                "!=" => CTokenKind.NotEqual,
                "<=" => CTokenKind.LessEqual,
                ">=" => CTokenKind.GreaterEqual,
                "<<" => CTokenKind.ShiftLeft,
                ">>" => CTokenKind.ShiftRight,
                "&&" => CTokenKind.LogicalAnd,
                "||" => CTokenKind.LogicalOr,
                "->" => CTokenKind.Arrow,
                "++" => CTokenKind.PlusPlus,
                "--" => CTokenKind.MinusMinus,
                "+=" => CTokenKind.PlusEqual,
                "-=" => CTokenKind.MinusEqual,
                "*=" => CTokenKind.StarEqual,
                "/=" => CTokenKind.SlashEqual,
                "%=" => CTokenKind.PercentEqual,
                "&=" => CTokenKind.AmpEqual,
                "|=" => CTokenKind.PipeEqual,
                "^=" => CTokenKind.CaretEqual,
                _ => (CTokenKind?)null,
            };

            if (twoCharKind.HasValue)
            {
                var token = this.MakeToken(twoCharKind.Value, twoChar);
                this.Advance();
                this.Advance();
                return token;
            }
        }

        var singleKind = ch switch
        {
            '(' => CTokenKind.LeftParen,
            ')' => CTokenKind.RightParen,
            '{' => CTokenKind.LeftBrace,
            '}' => CTokenKind.RightBrace,
            '[' => CTokenKind.LeftBracket,
            ']' => CTokenKind.RightBracket,
            ';' => CTokenKind.Semicolon,
            ',' => CTokenKind.Comma,
            '.' => CTokenKind.Dot,
            '+' => CTokenKind.Plus,
            '-' => CTokenKind.Minus,
            '*' => CTokenKind.Asterisk,
            '/' => CTokenKind.Slash,
            '%' => CTokenKind.Percent,
            '&' => CTokenKind.Ampersand,
            '|' => CTokenKind.Pipe,
            '^' => CTokenKind.Caret,
            '~' => CTokenKind.Tilde,
            '!' => CTokenKind.Exclamation,
            '<' => CTokenKind.Less,
            '>' => CTokenKind.Greater,
            '=' => CTokenKind.Assign,
            '?' => CTokenKind.Question,
            ':' => CTokenKind.Colon,
            _ => (CTokenKind?)null,
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

    private CToken ReadIntegerLiteral()
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
            return new CToken
            {
                Kind = CTokenKind.IntegerLiteral,
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
        return new CToken
        {
            Kind = CTokenKind.IntegerLiteral,
            Text = text,
            Line = startLine,
            Column = startCol,
            IntValue = int.Parse(text),
        };
    }

    private CToken ReadIdentifierOrKeyword()
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
        var kind = Keywords.TryGetValue(text, out var kw) ? kw : CTokenKind.Identifier;

        return new CToken
        {
            Kind = kind,
            Text = text,
            Line = startLine,
            Column = startCol,
        };
    }

    private CToken ReadCharLiteral()
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

        return new CToken
        {
            Kind = CTokenKind.CharLiteral,
            Text = $"'{ch}'",
            Line = startLine,
            Column = startCol,
            CharValue = ch,
        };
    }

    private CToken ReadStringLiteral()
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
        return new CToken
        {
            Kind = CTokenKind.StringLiteral,
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

            // Preprocessor directive — skip entire line (validated by TCC)
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

    private CToken MakeToken(CTokenKind kind, string text)
    {
        return new CToken
        {
            Kind = kind,
            Text = text,
            Line = this.line,
            Column = this.column,
        };
    }
}
