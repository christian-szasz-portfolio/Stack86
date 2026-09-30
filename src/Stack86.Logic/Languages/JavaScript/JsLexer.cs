namespace Stack86.Logic.Languages.JavaScript;

using System.Text;
using Stack86.Common.Exceptions;

/// <summary>
/// Tokenises JavaScript source code into a flat list of <see cref="JsToken"/> instances.
/// Supports single/double/template-string literals, <c>//</c> and <c>/* */</c> comments,
/// hex literals, and the arrow (<c>=&gt;</c>) operator.
/// </summary>
public sealed class JsLexer(string source)
{
    private static readonly Dictionary<string, JsTokenKind> Keywords = new()
    {
        ["var"] = JsTokenKind.VarKeyword,
        ["let"] = JsTokenKind.LetKeyword,
        ["const"] = JsTokenKind.ConstKeyword,
        ["function"] = JsTokenKind.FunctionKeyword,
        ["return"] = JsTokenKind.ReturnKeyword,
        ["if"] = JsTokenKind.IfKeyword,
        ["else"] = JsTokenKind.ElseKeyword,
        ["while"] = JsTokenKind.WhileKeyword,
        ["for"] = JsTokenKind.ForKeyword,
        ["do"] = JsTokenKind.DoKeyword,
        ["break"] = JsTokenKind.BreakKeyword,
        ["continue"] = JsTokenKind.ContinueKeyword,
        ["switch"] = JsTokenKind.SwitchKeyword,
        ["case"] = JsTokenKind.CaseKeyword,
        ["default"] = JsTokenKind.DefaultKeyword,
        ["true"] = JsTokenKind.TrueKeyword,
        ["false"] = JsTokenKind.FalseKeyword,
        ["null"] = JsTokenKind.NullKeyword,
        ["undefined"] = JsTokenKind.UndefinedKeyword,
        ["typeof"] = JsTokenKind.TypeofKeyword,
        ["of"] = JsTokenKind.OfKeyword,
        ["new"] = JsTokenKind.NewKeyword,
    };

    private int position;
    private int line = 1;
    private int column = 1;

    /// <summary>
    /// Tokenises the source into a list of tokens terminated by <see cref="JsTokenKind.EndOfFile"/>.
    /// </summary>
    public List<JsToken> Tokenize()
    {
        var tokens = new List<JsToken>();

        while (true)
        {
            var token = this.NextToken();
            tokens.Add(token);

            if (token.Kind == JsTokenKind.EndOfFile)
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

    private JsToken NextToken()
    {
        this.SkipWhitespaceAndComments();

        if (this.position >= source.Length)
        {
            return this.MakeToken(JsTokenKind.EndOfFile, string.Empty);
        }

        var ch = source[this.position];

        if (char.IsDigit(ch))
        {
            return this.ReadIntegerLiteral();
        }

        if (char.IsLetter(ch) || ch == '_' || ch == '$')
        {
            return this.ReadIdentifierOrKeyword();
        }

        if (ch is '"' or '\'')
        {
            return this.ReadStringLiteral(ch);
        }

        if (ch == '`')
        {
            return this.ReadTemplateLiteral();
        }

        // Three-character operators
        if (this.position + 2 < source.Length)
        {
            var threeChar = source.Substring(this.position, 3);
            var threeCharKind = threeChar switch
            {
                "===" => JsTokenKind.StrictEqual,
                "!==" => JsTokenKind.StrictNotEqual,
                "<<=" => JsTokenKind.ShiftLeftEqual,
                ">>=" => JsTokenKind.ShiftRightEqual,
                ">>>" => JsTokenKind.UnsignedShiftRight,
                _ => (JsTokenKind?)null,
            };

            if (threeCharKind.HasValue)
            {
                // Check for >>>= (four-char)
                if (threeChar == ">>>" && this.position + 3 < source.Length && source[this.position + 3] == '=')
                {
                    var token4 = this.MakeToken(JsTokenKind.UnsignedShiftRightEqual, ">>>=");
                    this.Advance();
                    this.Advance();
                    this.Advance();
                    this.Advance();
                    return token4;
                }

                var token = this.MakeToken(threeCharKind.Value, threeChar);
                this.Advance();
                this.Advance();
                this.Advance();
                return token;
            }
        }

        // Two-character operators
        if (this.position + 1 < source.Length)
        {
            var twoChar = source.Substring(this.position, 2);
            var twoCharKind = twoChar switch
            {
                "==" => JsTokenKind.Equal,
                "!=" => JsTokenKind.NotEqual,
                "<=" => JsTokenKind.LessEqual,
                ">=" => JsTokenKind.GreaterEqual,
                "<<" => JsTokenKind.ShiftLeft,
                ">>" => JsTokenKind.ShiftRight,
                "&&" => JsTokenKind.LogicalAnd,
                "||" => JsTokenKind.LogicalOr,
                "=>" => JsTokenKind.Arrow,
                "++" => JsTokenKind.PlusPlus,
                "--" => JsTokenKind.MinusMinus,
                "+=" => JsTokenKind.PlusEqual,
                "-=" => JsTokenKind.MinusEqual,
                "*=" => JsTokenKind.StarEqual,
                "/=" => JsTokenKind.SlashEqual,
                "%=" => JsTokenKind.PercentEqual,
                "&=" => JsTokenKind.AmpEqual,
                "|=" => JsTokenKind.PipeEqual,
                "^=" => JsTokenKind.CaretEqual,
                _ => (JsTokenKind?)null,
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
            '(' => JsTokenKind.LeftParen,
            ')' => JsTokenKind.RightParen,
            '{' => JsTokenKind.LeftBrace,
            '}' => JsTokenKind.RightBrace,
            '[' => JsTokenKind.LeftBracket,
            ']' => JsTokenKind.RightBracket,
            ';' => JsTokenKind.Semicolon,
            ',' => JsTokenKind.Comma,
            '.' => JsTokenKind.Dot,
            ':' => JsTokenKind.Colon,
            '?' => JsTokenKind.Question,
            '+' => JsTokenKind.Plus,
            '-' => JsTokenKind.Minus,
            '*' => JsTokenKind.Asterisk,
            '/' => JsTokenKind.Slash,
            '%' => JsTokenKind.Percent,
            '&' => JsTokenKind.Ampersand,
            '|' => JsTokenKind.Pipe,
            '^' => JsTokenKind.Caret,
            '~' => JsTokenKind.Tilde,
            '!' => JsTokenKind.Exclamation,
            '<' => JsTokenKind.Less,
            '>' => JsTokenKind.Greater,
            '=' => JsTokenKind.Assign,
            _ => (JsTokenKind?)null,
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

    private JsToken ReadIntegerLiteral()
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
            return new JsToken
            {
                Kind = JsTokenKind.IntegerLiteral,
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
        return new JsToken
        {
            Kind = JsTokenKind.IntegerLiteral,
            Text = text,
            Line = startLine,
            Column = startCol,
            IntValue = int.Parse(text),
        };
    }

    private JsToken ReadIdentifierOrKeyword()
    {
        var startLine = this.line;
        var startCol = this.column;
        var start = this.position;

        while (this.position < source.Length &&
               (char.IsLetterOrDigit(source[this.position]) || source[this.position] == '_' || source[this.position] == '$'))
        {
            this.Advance();
        }

        var text = source[start..this.position];
        var kind = Keywords.TryGetValue(text, out var kw) ? kw : JsTokenKind.Identifier;

        return new JsToken
        {
            Kind = kind,
            Text = text,
            Line = startLine,
            Column = startCol,
        };
    }

    private JsToken ReadStringLiteral(char quote)
    {
        var startLine = this.line;
        var startCol = this.column;
        this.Advance(); // skip opening quote

        var sb = new StringBuilder();

        while (this.position < source.Length && source[this.position] != quote)
        {
            sb.Append(this.ReadEscapedChar());
        }

        if (this.position >= source.Length)
        {
            throw new UnsupportedSyntaxException(
                $"Unterminated string literal at line {startLine}, column {startCol}.");
        }

        this.Advance(); // skip closing quote

        var value = sb.ToString();
        return new JsToken
        {
            Kind = JsTokenKind.StringLiteral,
            Text = $"{quote}{value}{quote}",
            Line = startLine,
            Column = startCol,
            StringValue = value,
        };
    }

    /// <summary>
    /// Reads a template literal starting at the opening backtick.
    /// Simple template literals without interpolations produce a single
    /// <see cref="JsTokenKind.TemplateLiteralFull"/> token.
    /// Template literals with <c>${...}</c> produce a sequence of
    /// Start / Middle / End tokens — but for simplicity in the 8086 target,
    /// we parse the entire template here and produce a single Full token
    /// whose <see cref="JsToken.StringValue"/> is the raw template text.
    /// The parser will handle <c>${}</c> splitting.
    /// </summary>
    private JsToken ReadTemplateLiteral()
    {
        var startLine = this.line;
        var startCol = this.column;
        this.Advance(); // skip opening `

        var sb = new StringBuilder();

        while (this.position < source.Length && source[this.position] != '`')
        {
            if (source[this.position] == '\\')
            {
                sb.Append(this.ReadEscapedChar());
            }
            else
            {
                sb.Append(source[this.position]);
                this.Advance();
            }
        }

        if (this.position >= source.Length)
        {
            throw new UnsupportedSyntaxException(
                $"Unterminated template literal at line {startLine}, column {startCol}.");
        }

        this.Advance(); // skip closing `

        var value = sb.ToString();
        return new JsToken
        {
            Kind = JsTokenKind.TemplateLiteralFull,
            Text = $"`{value}`",
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
                '`' => '`',
                '$' => '$',
                _ => escaped,
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

    private JsToken MakeToken(JsTokenKind kind, string text)
    {
        return new JsToken
        {
            Kind = kind,
            Text = text,
            Line = this.line,
            Column = this.column,
        };
    }
}
