import { describe, expect, it } from 'vitest';
import { InstructionRegistry } from './instruction-registry';

describe('InstructionRegistry', () => {
  const registry = new InstructionRegistry();

  it('registers core data movement opcodes', () => {
    for (const op of ['MOV', 'PUSH', 'POP', 'XCHG', 'LEA']) {
      expect(registry.has(op)).toBe(true);
    }
  });

  it('registers arithmetic opcodes', () => {
    for (const op of ['ADD', 'SUB', 'CMP', 'INC', 'DEC', 'MUL', 'DIV', 'NEG']) {
      expect(registry.has(op)).toBe(true);
    }
  });

  it('registers logic opcodes', () => {
    for (const op of ['AND', 'OR', 'XOR', 'NOT', 'SHL', 'SAL', 'SHR']) {
      expect(registry.has(op)).toBe(true);
    }
  });

  it('registers flow control opcodes', () => {
    for (const op of [
      'JMP', 'JE', 'JNE', 'JG', 'JGE', 'JL', 'JLE',
      'JA', 'JB', 'JC', 'JZ', 'JNZ', 'CALL', 'RET', 'LOOP',
    ]) {
      expect(registry.has(op)).toBe(true);
    }
  });

  it('registers misc opcodes (NOP, HLT, INT)', () => {
    expect(registry.has('NOP')).toBe(true);
    expect(registry.has('HLT')).toBe(true);
    expect(registry.has('INT')).toBe(true);
  });

  it('treats mnemonics case-insensitively', () => {
    expect(registry.has('mov')).toBe(true);
    expect(registry.has('Add')).toBe(true);
    expect(registry.get('jmp')).toBe(registry.get('JMP'));
  });

  it('returns undefined for an unknown mnemonic', () => {
    expect(registry.get('FOO')).toBeUndefined();
    expect(registry.has('FOO')).toBe(false);
  });

  it('exposes all mnemonics via getMnemonics()', () => {
    const list = registry.getMnemonics();
    expect(list.length).toBeGreaterThan(20);
    expect(list).toContain('MOV');
    expect(list).toContain('JMP');
  });

  it('SAL aliases to SHL handler', () => {
    expect(registry.get('SAL')).toBe(registry.get('SHL'));
  });

  it('exposes JZ, JNZ, JC as registered handlers', () => {
    expect(typeof registry.get('JZ')).toBe('function');
    expect(typeof registry.get('JNZ')).toBe('function');
    expect(typeof registry.get('JC')).toBe('function');
  });
});
