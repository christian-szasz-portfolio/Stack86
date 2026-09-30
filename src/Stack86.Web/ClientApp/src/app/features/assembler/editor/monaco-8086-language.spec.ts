import { describe, expect, it } from 'vitest';
import { ASM_8086_LANGUAGE_ID, asm8086LanguageDef, asm8086ThemeDef } from './monaco-8086-language';

describe('monaco-8086-language', () => {
  it('exports the language ID', () => {
    expect(ASM_8086_LANGUAGE_ID).toBe('asm8086');
  });

  it('language definition contains common 8086 keywords', () => {
    const def = asm8086LanguageDef as unknown as { keywords: string[] };
    expect(def.keywords).toEqual(expect.arrayContaining(['MOV', 'ADD', 'SUB', 'JMP', 'CALL', 'RET', 'INT']));
  });

  it('language definition lists the 8086 register names', () => {
    const def = asm8086LanguageDef as unknown as { registers: string[] };
    expect(def.registers).toEqual(expect.arrayContaining(['AX', 'BX', 'CX', 'DX', 'SP', 'BP', 'SI', 'DI']));
  });

  it('language definition uses case-insensitive tokenization', () => {
    expect(asm8086LanguageDef.ignoreCase).toBe(true);
  });

  it('theme inherits from vs-dark and provides keyword styling', () => {
    expect(asm8086ThemeDef.base).toBe('vs-dark');
    expect(asm8086ThemeDef.inherit).toBe(true);
    const keywordRule = asm8086ThemeDef.rules.find((r) => r.token === 'keyword');
    expect(keywordRule).toBeDefined();
    expect(keywordRule?.fontStyle).toBe('bold');
  });
});
