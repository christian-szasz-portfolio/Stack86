import type * as monaco from 'monaco-editor';

export const ASM_8086_LANGUAGE_ID = 'asm8086';

export const asm8086LanguageDef: monaco.languages.IMonarchLanguage = {
  ignoreCase: true,

  keywords: [
    'MOV', 'ADD', 'SUB', 'MUL', 'DIV', 'INC', 'DEC',
    'AND', 'OR', 'XOR', 'NOT', 'SHL', 'SHR', 'SAL',
    'CMP', 'JMP', 'JE', 'JNE', 'JG', 'JGE', 'JL', 'JLE',
    'JA', 'JB', 'JC', 'JZ', 'JNZ', 'LOOP',
    'CALL', 'RET', 'PUSH', 'POP', 'XCHG', 'LEA',
    'NOP', 'HLT', 'INT', 'DB', 'DW',
    'PROC', 'ENDP', 'STRUC', 'ENDS',
    'NEG', 'REP', 'MOVSB',
  ],

  registers: [
    'AX', 'BX', 'CX', 'DX', 'SP', 'BP', 'SI', 'DI', 'IP',
    'AH', 'AL', 'BH', 'BL', 'CH', 'CL', 'DH', 'DL',
  ],

  sizeModifiers: ['BYTE', 'WORD', 'PTR'],

  tokenizer: {
    root: [
      // Comments
      [/;.*$/, 'comment'],

      // String literals
      [/"[^"]*"/, 'string'],
      [/'[^']*'/, 'string'],

      // Hex numbers (0x prefix)
      [/0[xX][0-9a-fA-F]+/, 'number.hex'],

      // Hex numbers (h suffix)
      [/[0-9][0-9a-fA-F]*[hH]/, 'number.hex'],

      // Binary numbers (b suffix)
      [/[01]+[bB]/, 'number.binary'],

      // Decimal numbers
      [/\d+/, 'number'],

      // Labels (identifier followed by colon)
      [/[a-zA-Z_]\w*(?=\s*:)/, 'type.identifier'],

      // Colons
      [/:/, 'delimiter'],

      // Identifiers & keywords
      [/[a-zA-Z_]\w*/, {
        cases: {
          '@keywords': 'keyword',
          '@registers': 'variable.predefined',
          '@sizeModifiers': 'keyword.modifier',
          '@default': 'identifier',
        },
      }],

      // Brackets
      [/[[\]]/, 'delimiter.bracket'],

      // Comma
      [/,/, 'delimiter'],

      // Whitespace
      [/\s+/, 'white'],
    ],
  },
};

export const asm8086ThemeDef: monaco.editor.IStandaloneThemeData = {
  base: 'vs-dark',
  inherit: true,
  rules: [
    { token: 'keyword', foreground: '569CD6', fontStyle: 'bold' },
    { token: 'keyword.modifier', foreground: '4EC9B0' },
    { token: 'variable.predefined', foreground: '9CDCFE' },
    { token: 'number', foreground: 'B5CEA8' },
    { token: 'number.hex', foreground: 'B5CEA8' },
    { token: 'number.binary', foreground: 'B5CEA8' },
    { token: 'comment', foreground: '6A9955', fontStyle: 'italic' },
    { token: 'string', foreground: 'CE9178' },
    { token: 'type.identifier', foreground: 'DCDCAA' },
    { token: 'delimiter', foreground: 'D4D4D4' },
    { token: 'delimiter.bracket', foreground: 'FFD700' },
    { token: 'identifier', foreground: 'D4D4D4' },
  ],
  colors: {},
};
