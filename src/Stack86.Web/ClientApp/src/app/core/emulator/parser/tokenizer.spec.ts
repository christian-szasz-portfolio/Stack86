import { describe, it, expect } from 'vitest';
import { Tokenizer, TokenType } from './tokenizer';

describe('Tokenizer', () => {
  const tokenizer = new Tokenizer();

  it('should tokenize a simple MOV instruction', () => {
    const result = tokenizer.tokenize('MOV AX, 5');
    expect(result).toHaveLength(1);
    const tokens = result[0].tokens;
    expect(tokens[0]).toMatchObject({ type: TokenType.Identifier, value: 'MOV' });
    expect(tokens[1]).toMatchObject({ type: TokenType.Register, value: 'ax' });
    expect(tokens[2]).toMatchObject({ type: TokenType.Comma, value: ',' });
    expect(tokens[3]).toMatchObject({ type: TokenType.Number, value: '5' });
  });

  it('should strip comments', () => {
    const result = tokenizer.tokenize('MOV AX, 1 ; set AX');
    expect(result).toHaveLength(1);
    const tokens = result[0].tokens;
    expect(tokens).toHaveLength(4);
  });

  it('should skip empty and comment-only lines', () => {
    const result = tokenizer.tokenize('; comment\n\n  \n  ; another');
    expect(result).toHaveLength(0);
  });

  it('should tokenize hex numbers with 0x prefix', () => {
    const result = tokenizer.tokenize('MOV AX, 0xFF');
    const numToken = result[0].tokens[3];
    expect(numToken).toMatchObject({ type: TokenType.Number, value: '0xFF' });
  });

  it('should tokenize hex numbers with h suffix', () => {
    const result = tokenizer.tokenize('MOV AX, 0FFh');
    const numToken = result[0].tokens[3];
    expect(numToken).toMatchObject({ type: TokenType.Number, value: '0FFh' });
  });

  it('should tokenize labels with colon', () => {
    const result = tokenizer.tokenize('start: MOV AX, 0');
    const tokens = result[0].tokens;
    expect(tokens[0]).toMatchObject({ type: TokenType.Identifier, value: 'start' });
    expect(tokens[1]).toMatchObject({ type: TokenType.Colon, value: ':' });
    expect(tokens[2]).toMatchObject({ type: TokenType.Identifier, value: 'MOV' });
  });

  it('should tokenize memory references with brackets', () => {
    const result = tokenizer.tokenize('MOV [BX], AX');
    const tokens = result[0].tokens;
    expect(tokens[1]).toMatchObject({ type: TokenType.OpenBracket });
    expect(tokens[2]).toMatchObject({ type: TokenType.Register, value: 'bx' });
    expect(tokens[3]).toMatchObject({ type: TokenType.CloseBracket });
  });

  it('should tokenize BYTE PTR prefix', () => {
    const result = tokenizer.tokenize('MOV BYTE PTR [BX], 5');
    const tokens = result[0].tokens;
    expect(tokens[1]).toMatchObject({ type: TokenType.SizePrefix, value: 'byte' });
  });

  it('should tokenize 8-bit registers', () => {
    const result = tokenizer.tokenize('MOV AH, AL');
    const tokens = result[0].tokens;
    expect(tokens[1]).toMatchObject({ type: TokenType.Register, value: 'ah' });
    expect(tokens[3]).toMatchObject({ type: TokenType.Register, value: 'al' });
  });

  it('should tokenize string literals', () => {
    const result = tokenizer.tokenize('DB "Hello"');
    const tokens = result[0].tokens;
    expect(tokens[1]).toMatchObject({ type: TokenType.String, value: 'Hello' });
  });

  it('should preserve line numbers across multiple lines', () => {
    const result = tokenizer.tokenize('MOV AX, 1\n; comment\nADD AX, 2');
    expect(result).toHaveLength(2);
    expect(result[0].line).toBe(0);
    expect(result[1].line).toBe(2);
  });

  it('should preserve original source text', () => {
    const result = tokenizer.tokenize('  MOV  AX,  1  ; set');
    expect(result[0].source).toBe('  MOV  AX,  1  ; set');
  });

  it('should tokenize plus and minus operators inside brackets', () => {
    const result = tokenizer.tokenize('MOV [BP-2], AX');
    const tokens = result[0].tokens;
    expect(tokens[1]).toMatchObject({ type: TokenType.OpenBracket });
    expect(tokens[2]).toMatchObject({ type: TokenType.Register, value: 'bp' });
    expect(tokens[3]).toMatchObject({ type: TokenType.Minus, value: '-' });
    expect(tokens[4]).toMatchObject({ type: TokenType.Number, value: '2' });
    expect(tokens[5]).toMatchObject({ type: TokenType.CloseBracket });
  });

  it('should tokenize plus operator in memory reference', () => {
    const result = tokenizer.tokenize('MOV [BX+4], AX');
    const tokens = result[0].tokens;
    expect(tokens[2]).toMatchObject({ type: TokenType.Register, value: 'bx' });
    expect(tokens[3]).toMatchObject({ type: TokenType.Plus, value: '+' });
    expect(tokens[4]).toMatchObject({ type: TokenType.Number, value: '4' });
  });

  it('should tokenize question mark for STRUC field placeholders', () => {
    const result = tokenizer.tokenize('x DW ?');
    const tokens = result[0].tokens;
    expect(tokens[0]).toMatchObject({ type: TokenType.Identifier, value: 'x' });
    expect(tokens[1]).toMatchObject({ type: TokenType.Identifier, value: 'DW' });
    expect(tokens[2]).toMatchObject({ type: TokenType.QuestionMark, value: '?' });
  });
});
