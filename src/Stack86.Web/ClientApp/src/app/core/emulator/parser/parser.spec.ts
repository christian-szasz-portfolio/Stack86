import { describe, it, expect } from 'vitest';
import { Parser, MemoryModel } from './parser';
import { OperandType, OperandSize } from '../instruction/instruction.model';

describe('Parser', () => {
  const parser = new Parser();

  describe('basic instructions', () => {
    it('should parse a single MOV instruction', () => {
      const result = parser.parse('MOV AX, 5');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions).toHaveLength(1);
      const inst = result.instructions[0];
      expect(inst.mnemonic).toBe('MOV');
      expect(inst.operands).toHaveLength(2);
      expect(inst.operands[0]).toMatchObject({ type: OperandType.Register, value: 'ax' });
      expect(inst.operands[1]).toMatchObject({ type: OperandType.Immediate, value: 5 });
    });

    it('should parse register-to-register MOV', () => {
      const result = parser.parse('MOV BX, CX');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions[0].operands[0]).toMatchObject({ type: OperandType.Register, value: 'bx' });
      expect(result.instructions[0].operands[1]).toMatchObject({ type: OperandType.Register, value: 'cx' });
    });

    it('should parse hex immediate with 0x prefix', () => {
      const result = parser.parse('MOV AX, 0xFF');
      expect(result.instructions[0].operands[1]).toMatchObject({ type: OperandType.Immediate, value: 255 });
    });

    it('should parse hex immediate with h suffix', () => {
      const result = parser.parse('MOV AX, 0FFh');
      expect(result.instructions[0].operands[1]).toMatchObject({ type: OperandType.Immediate, value: 255 });
    });

    it('should parse single-operand instructions', () => {
      const result = parser.parse('INC AX');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions[0].mnemonic).toBe('INC');
      expect(result.instructions[0].operands).toHaveLength(1);
    });

    it('should parse no-operand instructions', () => {
      const result = parser.parse('NOP\nHLT');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions).toHaveLength(2);
      expect(result.instructions[0].mnemonic).toBe('NOP');
      expect(result.instructions[1].mnemonic).toBe('HLT');
    });
  });

  describe('memory operands', () => {
    it('should parse memory reference [BX]', () => {
      const result = parser.parse('MOV [BX], AX');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions[0].operands[0]).toMatchObject({
        type: OperandType.Memory,
        value: 'bx',
      });
    });

    it('should parse memory reference with size prefix', () => {
      const result = parser.parse('MOV BYTE PTR [BX], 5');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions[0].operands[0]).toMatchObject({
        type: OperandType.Memory,
        value: 'bx',
        size: OperandSize.Byte,
      });
    });

    it('should parse [BP-2] as register with negative offset', () => {
      const result = parser.parse('MOV [BP-2], AX');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions[0].operands[0]).toMatchObject({
        type: OperandType.Memory,
        value: 'bp',
        offset: -2,
      });
    });

    it('should parse [BX+4] as register with positive offset', () => {
      const result = parser.parse('MOV [BX+4], AX');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions[0].operands[0]).toMatchObject({
        type: OperandType.Memory,
        value: 'bx',
        offset: 4,
      });
    });

    it('should parse [BP] without offset', () => {
      const result = parser.parse('MOV [BP], AX');
      expect(result.errors).toHaveLength(0);
      const op = result.instructions[0].operands[0];
      expect(op).toMatchObject({ type: OperandType.Memory, value: 'bp' });
      expect(op.offset).toBeUndefined();
    });
  });

  describe('labels', () => {
    it('should parse label definitions and resolve jumps', () => {
      const source = `
start: MOV AX, 1
       ADD AX, 2
       JMP start
`;
      const result = parser.parse(source);
      expect(result.errors).toHaveLength(0);
      expect(result.labels.get('start')).toBe(0);
      expect(result.instructions).toHaveLength(3);
      // JMP operand should be a label reference
      expect(result.instructions[2].operands[0]).toMatchObject({
        type: OperandType.Label,
        value: 'start',
      });
    });

    it('should handle label-only lines', () => {
      const source = `
loop:
  MOV AX, 0
  JMP loop
`;
      const result = parser.parse(source);
      expect(result.errors).toHaveLength(0);
      expect(result.labels.get('loop')).toBe(0);
      expect(result.instructions).toHaveLength(2);
    });

    it('should detect duplicate labels', () => {
      const source = `
start: NOP
start: NOP
`;
      const result = parser.parse(source);
      expect(result.errors.length).toBeGreaterThan(0);
      expect(result.errors[0].message).toContain('Duplicate label');
    });

    it('should handle forward label references', () => {
      const source = `
  JMP end
  MOV AX, 1
end: HLT
`;
      const result = parser.parse(source);
      expect(result.errors).toHaveLength(0);
      expect(result.labels.get('end')).toBe(2);
      expect(result.instructions[0].operands[0]).toMatchObject({
        type: OperandType.Label,
        value: 'end',
      });
    });
  });

  describe('multi-line programs', () => {
    it('should assign sequential addresses', () => {
      const result = parser.parse('MOV AX, 0\nADD AX, 1\nSUB AX, 2');
      expect(result.instructions[0].address).toBe(0);
      expect(result.instructions[1].address).toBe(1);
      expect(result.instructions[2].address).toBe(2);
    });

    it('should preserve source line numbers', () => {
      const source = '; comment\nMOV AX, 1\n\nADD AX, 2';
      const result = parser.parse(source);
      expect(result.instructions[0].line).toBe(1);
      expect(result.instructions[1].line).toBe(3);
    });
  });

  describe('error handling', () => {
    it('should report unknown instructions', () => {
      const result = parser.parse('FOOBAR AX');
      expect(result.errors.length).toBeGreaterThan(0);
      expect(result.errors[0].message).toContain('Unknown instruction');
    });
  });

  describe('comments and whitespace', () => {
    it('should ignore inline comments', () => {
      const result = parser.parse('MOV AX, 5 ; load 5 into AX');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions).toHaveLength(1);
    });

    it('should handle leading/trailing whitespace', () => {
      const result = parser.parse('   MOV   AX ,  5   ');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions).toHaveLength(1);
    });
  });

  describe('assembler directives', () => {
    it('should parse .MODEL SMALL and set config', () => {
      const result = parser.parse('.MODEL SMALL\nMOV AX, 5');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions).toHaveLength(1);
      expect(result.instructions[0].mnemonic).toBe('MOV');
      expect(result.config.hasModel).toBe(true);
      expect(result.config.model).toBe(MemoryModel.Small);
      expect(result.config.stackSize).toBe(256);
    });

    it('should parse .STACK with explicit size', () => {
      const result = parser.parse('.MODEL SMALL\n.STACK 512\nMOV AX, 5');
      expect(result.errors).toHaveLength(0);
      expect(result.config.stackSize).toBe(512);
    });

    it('should set default stack size based on model', () => {
      const result = parser.parse('.MODEL TINY\nMOV AX, 5');
      expect(result.config.model).toBe(MemoryModel.Tiny);
      expect(result.config.stackSize).toBe(128);
    });

    it('should not set hasModel when no .MODEL directive', () => {
      const result = parser.parse('MOV AX, 5');
      expect(result.config.hasModel).toBe(false);
    });

    it('should skip .CODE directive', () => {
      const result = parser.parse('.CODE\nMOV AX, 5');
      expect(result.errors).toHaveLength(0);
      expect(result.instructions).toHaveLength(1);
    });

    it('should handle full program with directives', () => {
      const source = [
        '.MODEL SMALL',
        '.STACK 256',
        '.CODE',
        'main:',
        '  MOV AX, @DATA',
        '  MOV DS, AX',
        '  MOV AX, 42',
        '  HLT',
        '@DATA:',
        'msg: DB 65, 66, 0',
      ].join('\n');
      const result = parser.parse(source);
      expect(result.errors).toHaveLength(0);
      // Directives are excluded from instructions
      expect(result.instructions).toHaveLength(5); // MOV, MOV, MOV, HLT, DB
      expect(result.labels.get('main')).toBe(0);
      expect(result.labels.has('@DATA')).toBe(true);
      expect(result.config.hasModel).toBe(true);
      expect(result.config.stackSize).toBe(256);
    });
  });

  describe('PROC/ENDP directives', () => {
    it('should treat name PROC as a label', () => {
      const source = [
        '.CODE',
        'main PROC',
        '  MOV AX, 1',
        '  HLT',
        'main ENDP',
      ].join('\n');
      const result = parser.parse(source);
      expect(result.errors).toHaveLength(0);
      expect(result.labels.get('main')).toBe(0);
      expect(result.instructions).toHaveLength(2);
    });

    it('should support multiple procedures', () => {
      const source = [
        'helper PROC',
        '  MOV AX, 5',
        '  RET',
        'helper ENDP',
        'main PROC',
        '  CALL helper',
        '  HLT',
        'main ENDP',
      ].join('\n');
      const result = parser.parse(source);
      expect(result.errors).toHaveLength(0);
      expect(result.labels.get('helper')).toBe(0);
      expect(result.labels.get('main')).toBe(2);
      expect(result.instructions).toHaveLength(4);
    });

    it('should report duplicate PROC labels', () => {
      const source = [
        'main PROC',
        '  NOP',
        'main ENDP',
        'main PROC',
        '  NOP',
        'main ENDP',
      ].join('\n');
      const result = parser.parse(source);
      expect(result.errors.length).toBeGreaterThan(0);
      expect(result.errors[0].message).toContain('Duplicate label');
    });
  });

  describe('STRUC/ENDS directives', () => {
    it('should parse a struct definition', () => {
      const source = [
        'Point STRUC',
        '  x DW ?',
        '  y DW ?',
        'Point ENDS',
        'MOV AX, 5',
      ].join('\n');
      const result = parser.parse(source);
      expect(result.errors).toHaveLength(0);
      expect(result.instructions).toHaveLength(1);
      expect(result.structs).toHaveLength(1);
      expect(result.structs[0].name).toBe('Point');
      expect(result.structs[0].fields).toHaveLength(2);
      expect(result.structs[0].fields[0]).toEqual({ name: 'x', size: 2 });
      expect(result.structs[0].fields[1]).toEqual({ name: 'y', size: 2 });
    });

    it('should handle DB fields in structs', () => {
      const source = [
        'Reg STRUC',
        '  lo DB ?',
        '  hi DB ?',
        'Reg ENDS',
        'NOP',
      ].join('\n');
      const result = parser.parse(source);
      expect(result.errors).toHaveLength(0);
      expect(result.structs[0].fields[0]).toEqual({ name: 'lo', size: 1 });
      expect(result.structs[0].fields[1]).toEqual({ name: 'hi', size: 1 });
    });

    it('should not emit instructions for struct body lines', () => {
      const source = [
        'Point STRUC',
        '  x DW ?',
        '  y DW ?',
        'Point ENDS',
      ].join('\n');
      const result = parser.parse(source);
      expect(result.instructions).toHaveLength(0);
    });

    it('should handle struct and proc together', () => {
      const source = [
        'Point STRUC',
        '  x DW ?',
        '  y DW ?',
        'Point ENDS',
        'main PROC',
        '  MOV AX, 1',
        '  HLT',
        'main ENDP',
      ].join('\n');
      const result = parser.parse(source);
      expect(result.errors).toHaveLength(0);
      expect(result.structs).toHaveLength(1);
      expect(result.labels.get('main')).toBe(0);
      expect(result.instructions).toHaveLength(2);
    });
  });
});
