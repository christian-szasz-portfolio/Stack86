export enum TokenType {
  Label = 'label',
  Mnemonic = 'mnemonic',
  Register = 'register',
  Number = 'number',
  Comma = 'comma',
  OpenBracket = 'open_bracket',
  CloseBracket = 'close_bracket',
  Identifier = 'identifier',
  String = 'string',
  SizePrefix = 'size_prefix',
  Colon = 'colon',
  Directive = 'directive',
  Plus = 'plus',
  Minus = 'minus',
  QuestionMark = 'question_mark',
}

export interface Token {
  type: TokenType;
  value: string;
  line: number;
  column: number;
}

export interface TokenizedLine {
  tokens: Token[];
  line: number;
  source: string;
}

const REGISTERS = new Set([
  'ax', 'bx', 'cx', 'dx', 'sp', 'bp', 'si', 'di', 'ip',
  'ah', 'al', 'bh', 'bl', 'ch', 'cl', 'dh', 'dl',
  'cs', 'ds', 'es', 'ss',
]);

const SIZE_PREFIXES = new Set(['byte', 'word']);

export class Tokenizer {
  public tokenize(source: string): TokenizedLine[] {
    const lines = source.split(/\r?\n/);
    const result: TokenizedLine[] = [];

    for (let i = 0; i < lines.length; i++) {
      const raw = lines[i];
      const stripped = Tokenizer.stripComment(raw);
      const trimmed = stripped.trim();
      if (trimmed.length === 0) continue;

      const tokens = Tokenizer.tokenizeLine(trimmed, i);
      if (tokens.length > 0) {
        result.push({ tokens, line: i, source: raw });
      }
    }

    return result;
  }

  private static stripComment(line: string): string {
    let inString = false;
    let stringChar = '';
    for (let i = 0; i < line.length; i++) {
      const ch = line[i];
      if (inString) {
        if (ch === stringChar) inString = false;
      } else if (ch === '"' || ch === "'") {
        inString = true;
        stringChar = ch;
      } else if (ch === ';') {
        return line.substring(0, i);
      }
    }
    return line;
  }

  private static tokenizeLine(line: string, lineNum: number): Token[] {
    const tokens: Token[] = [];
    let pos = 0;

    while (pos < line.length) {
      // Skip whitespace
      if (/\s/.test(line[pos])) {
        pos++;
        continue;
      }

      const col = pos;
      const ch = line[pos];

      // Comma
      if (ch === ',') {
        tokens.push({ type: TokenType.Comma, value: ',', line: lineNum, column: col });
        pos++;
        continue;
      }

      // Brackets for memory addressing
      if (ch === '[') {
        tokens.push({ type: TokenType.OpenBracket, value: '[', line: lineNum, column: col });
        pos++;
        continue;
      }
      if (ch === ']') {
        tokens.push({ type: TokenType.CloseBracket, value: ']', line: lineNum, column: col });
        pos++;
        continue;
      }

      // Colon (label definition)
      if (ch === ':') {
        tokens.push({ type: TokenType.Colon, value: ':', line: lineNum, column: col });
        pos++;
        continue;
      }

      // String literal
      if (ch === '"' || ch === "'") {
        const quote = ch;
        let str = '';
        pos++; // skip opening quote
        while (pos < line.length && line[pos] !== quote) {
          str += line[pos];
          pos++;
        }
        pos++; // skip closing quote
        tokens.push({ type: TokenType.String, value: str, line: lineNum, column: col });
        continue;
      }

      // Number: hex (0x.., ..h) or decimal
      if (/[0-9]/.test(ch)) {
        let num = '';
        const start = pos;
        while (pos < line.length && /[0-9a-fA-Fx]/.test(line[pos])) {
          num += line[pos];
          pos++;
        }
        // Check for 'h' suffix (e.g. 0FFh)
        if (pos < line.length && line[pos].toLowerCase() === 'h') {
          num += line[pos];
          pos++;
        }
        tokens.push({ type: TokenType.Number, value: num, line: lineNum, column: start });
        continue;
      }

      // Assembler directive (e.g. .MODEL, .CODE, .DATA)
      if (ch === '.') {
        let word = '';
        const start = pos;
        pos++; // skip the dot
        while (pos < line.length && /[a-zA-Z0-9_]/.test(line[pos])) {
          word += line[pos];
          pos++;
        }
        tokens.push({ type: TokenType.Directive, value: '.' + word.toUpperCase(), line: lineNum, column: start });
        continue;
      }

      // Identifier / keyword / register (also handles @-prefixed identifiers like @DATA)
      if (/[a-zA-Z_@]/.test(ch)) {
        let word = '';
        const start = pos;
        while (pos < line.length && /[a-zA-Z0-9_@]/.test(line[pos])) {
          word += line[pos];
          pos++;
        }
        const lower = word.toLowerCase();

        // Check for 'h' suffix on hex numbers starting with letter (e.g. FFh, 0Ah)
        // Nope — that would have started with 0-9 above.

        if (SIZE_PREFIXES.has(lower)) {
          // "BYTE" or "WORD" — check if followed by "PTR"
          const rest = line.substring(pos).trimStart();
          if (rest.toLowerCase().startsWith('ptr')) {
            pos += line.substring(pos).indexOf(rest[0]) + 3;
            tokens.push({ type: TokenType.SizePrefix, value: lower, line: lineNum, column: start });
            continue;
          }
          // standalone "byte" / "word" treated as size prefix anyway
          tokens.push({ type: TokenType.SizePrefix, value: lower, line: lineNum, column: start });
          continue;
        }

        if (REGISTERS.has(lower)) {
          tokens.push({ type: TokenType.Register, value: lower, line: lineNum, column: start });
          continue;
        }

        // Check if it's a label definition (followed by colon)
        // We'll let the parser handle the colon — just emit as identifier
        tokens.push({ type: TokenType.Identifier, value: word, line: lineNum, column: start });
        continue;
      }

      // Arithmetic operators (for memory expressions like [BP-2])
      if (ch === '+') {
        tokens.push({ type: TokenType.Plus, value: '+', line: lineNum, column: col });
        pos++;
        continue;
      }
      if (ch === '-') {
        tokens.push({ type: TokenType.Minus, value: '-', line: lineNum, column: col });
        pos++;
        continue;
      }
      if (ch === '?') {
        tokens.push({ type: TokenType.QuestionMark, value: '?', line: lineNum, column: col });
        pos++;
        continue;
      }

      // Unknown character — skip
      pos++;
    }

    return tokens;
  }
}
