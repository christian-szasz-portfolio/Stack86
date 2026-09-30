import { describe, it, expect } from 'vitest';
import { X86Encoder } from './x86-encoder';
import { X86Decoder } from './x86-decoder';
import { ParsedInstruction, Operand, OperandType, OperandSize } from '../instruction/instruction.model';
import { ParseResult, MemoryModel, ProgramConfig } from '../parser/parser';
import { Parser } from '../parser/parser';

/**
 * Helper to build a minimal ParseResult for encoding tests.
 */
function buildParseResult(instructions: ParsedInstruction[]): ParseResult {
  const labels = new Map<string, number>();
  const config: ProgramConfig = { hasModel: false, model: MemoryModel.Small, stackSize: 256 };
  return {
    instructions,
    labels,
    errors: [],
    config,
    structs: [],
  };
}

function makeOp(type: OperandType, value: string | number, size?: OperandSize, offset?: number): Operand {
  return { type, value, size, offset };
}

function makeInstr(
  mnemonic: string,
  operands: Operand[],
  address: number,
  size: number = 1,
): ParsedInstruction {
  return { mnemonic, operands, line: 0, address, size, source: '' };
}

describe('X86Encoder', () => {
  const encoder = new X86Encoder();

  describe('simple instructions', () => {
    it('should encode NOP', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('NOP', [], 0),
      ]));
      expect(Array.from(result.bytes)).toEqual([0x90]);
    });

    it('should encode HLT', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('HLT', [], 0),
      ]));
      expect(Array.from(result.bytes)).toEqual([0xF4]);
    });

    it('should encode RET', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('RET', [], 0),
      ]));
      expect(Array.from(result.bytes)).toEqual([0xC3]);
    });

    it('should encode INT 21h', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('INT', [makeOp(OperandType.Immediate, 0x21)], 0),
      ]));
      expect(Array.from(result.bytes)).toEqual([0xCD, 0x21]);
    });
  });

  describe('MOV encoding', () => {
    it('should encode MOV AX, 1234h (reg16, imm16)', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('MOV', [
          makeOp(OperandType.Register, 'ax'),
          makeOp(OperandType.Immediate, 0x1234),
        ], 0),
      ]));
      expect(Array.from(result.bytes)).toEqual([0xB8, 0x34, 0x12]);
    });

    it('should encode MOV AL, 42h (reg8, imm8)', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('MOV', [
          makeOp(OperandType.Register, 'al'),
          makeOp(OperandType.Immediate, 0x42),
        ], 0),
      ]));
      expect(Array.from(result.bytes)).toEqual([0xB0, 0x42]);
    });

    it('should encode MOV AX, BX (reg16, reg16)', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('MOV', [
          makeOp(OperandType.Register, 'ax'),
          makeOp(OperandType.Register, 'bx'),
        ], 0),
      ]));
      // 8Bh /r with mod=11 reg=AX(0) rm=BX(3) → 8B C3
      expect(result.bytes[0]).toBe(0x8B);
    });
  });

  describe('INC / DEC encoding', () => {
    it('should encode INC AX (short form 40h)', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('INC', [makeOp(OperandType.Register, 'ax')], 0),
      ]));
      expect(Array.from(result.bytes)).toEqual([0x40]);
    });

    it('should encode DEC BX (short form 4Bh)', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('DEC', [makeOp(OperandType.Register, 'bx')], 0),
      ]));
      expect(Array.from(result.bytes)).toEqual([0x4B]);
    });
  });

  describe('PUSH / POP encoding', () => {
    it('should encode PUSH AX', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('PUSH', [makeOp(OperandType.Register, 'ax')], 0),
      ]));
      expect(Array.from(result.bytes)).toEqual([0x50]);
    });

    it('should encode POP BX', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('POP', [makeOp(OperandType.Register, 'bx')], 0),
      ]));
      expect(Array.from(result.bytes)).toEqual([0x5B]);
    });

    it('should encode PUSH imm8', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('PUSH', [makeOp(OperandType.Immediate, 5)], 0),
      ]));
      expect(Array.from(result.bytes)).toEqual([0x6A, 0x05]);
    });
  });

  describe('ALU encoding', () => {
    it('should encode ADD AX, 1 (sign-extended imm8)', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('ADD', [
          makeOp(OperandType.Register, 'ax'),
          makeOp(OperandType.Immediate, 1),
        ], 0),
      ]));
      // 83h /0 mod=11 rm=AX(0) imm8=01
      expect(result.bytes[0]).toBe(0x83);
      expect(result.bytes[result.bytes.length - 1]).toBe(0x01);
    });

    it('should encode SUB BX, CX (reg, reg)', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('SUB', [
          makeOp(OperandType.Register, 'bx'),
          makeOp(OperandType.Register, 'cx'),
        ], 0),
      ]));
      // 2Bh /r direction bit set → reg is dst
      expect(result.bytes[0]).toBe(0x2B);
    });
  });

  describe('DB / DW directives', () => {
    it('should encode DB with string', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('DB', [makeOp(OperandType.Immediate, 'Hello$')], 0, 6),
      ]));
      expect(Array.from(result.bytes)).toEqual([72, 101, 108, 108, 111, 36]);
    });

    it('should encode DW with word value', () => {
      const result = encoder.encode(buildParseResult([
        makeInstr('DW', [makeOp(OperandType.Immediate, 0x1234)], 0, 2),
      ]));
      expect(Array.from(result.bytes)).toEqual([0x34, 0x12]);
    });
  });
});

describe('X86Decoder', () => {
  const decoder = new X86Decoder();

  describe('simple instructions', () => {
    it('should decode NOP', () => {
      const source = decoder.decode(new Uint8Array([0x90]));
      expect(source).toContain('NOP');
    });

    it('should decode HLT', () => {
      const source = decoder.decode(new Uint8Array([0xF4]));
      expect(source).toContain('HLT');
    });

    it('should decode RET', () => {
      const source = decoder.decode(new Uint8Array([0xC3]));
      expect(source).toContain('RET');
    });

    it('should decode INT 21h', () => {
      const source = decoder.decode(new Uint8Array([0xCD, 0x21]));
      expect(source).toContain('INT');
      expect(source).toContain('21');
    });
  });

  describe('MOV decoding', () => {
    it('should decode MOV AX, imm16', () => {
      const source = decoder.decode(new Uint8Array([0xB8, 0x34, 0x12]));
      expect(source).toContain('MOV');
      expect(source).toContain('AX');
    });

    it('should decode MOV AL, imm8', () => {
      const source = decoder.decode(new Uint8Array([0xB0, 0x42]));
      expect(source).toContain('MOV');
      expect(source).toContain('AL');
    });
  });

  describe('INC / DEC decoding', () => {
    it('should decode INC AX (40h)', () => {
      const source = decoder.decode(new Uint8Array([0x40]));
      expect(source).toContain('INC');
      expect(source).toContain('AX');
    });

    it('should decode DEC BX (4Bh)', () => {
      const source = decoder.decode(new Uint8Array([0x4B]));
      expect(source).toContain('DEC');
      expect(source).toContain('BX');
    });
  });

  describe('PUSH / POP decoding', () => {
    it('should decode PUSH AX (50h)', () => {
      const source = decoder.decode(new Uint8Array([0x50]));
      expect(source).toContain('PUSH');
      expect(source).toContain('AX');
    });

    it('should decode POP BX (5Bh)', () => {
      const source = decoder.decode(new Uint8Array([0x5B]));
      expect(source).toContain('POP');
      expect(source).toContain('BX');
    });
  });

  describe('jump label generation', () => {
    it('should generate labels for short jumps', () => {
      // JMP +0 (jump to self) at offset 0 → target = 2+0 = 2, but only 2 bytes total
      // Let's do: EB 00 90 → JMP to offset 2 (the NOP)
      const source = decoder.decode(new Uint8Array([0xEB, 0x00, 0x90]));
      expect(source).toContain('JMP');
      expect(source).toContain('L_0002');
    });

    it('should generate labels for conditional jumps', () => {
      // JE +0 at offset 0 → target at offset 2, then NOP at offset 2
      const source = decoder.decode(new Uint8Array([0x74, 0x00, 0x90]));
      expect(source).toContain('JE');
      expect(source).toContain('L_0002');
    });
  });

  describe('unknown opcodes', () => {
    it('should emit DB for unrecognised bytes', () => {
      // 0x0F is not a simple single-byte opcode we handle
      const source = decoder.decode(new Uint8Array([0x0F]));
      expect(source).toContain('DB');
    });
  });
});

describe('Encoder/Decoder round-trip', () => {
  const encoder = new X86Encoder();
  const decoder = new X86Decoder();
  const parser = new Parser();

  it('should round-trip a simple program through encode → decode', () => {
    const source = `.MODEL SMALL
.STACK 256
.CODE
    MOV AX, 1
    MOV BX, 2
    ADD AX, BX
    HLT`;

    const result = parser.parse(source);
    expect(result.errors).toHaveLength(0);

    const encoded = encoder.encode(result);
    expect(encoded.bytes.length).toBeGreaterThan(0);

    const decoded = decoder.decode(encoded.bytes);
    expect(decoded).toContain('MOV');
    expect(decoded).toContain('ADD');
    expect(decoded).toContain('HLT');
  });

  it('should round-trip NOP + INT 21h + HLT', () => {
    const result = buildParseResult([
      makeInstr('NOP', [], 0),
      makeInstr('INT', [makeOp(OperandType.Immediate, 0x21)], 1),
      makeInstr('HLT', [], 2),
    ]);

    const encoded = encoder.encode(result);
    expect(Array.from(encoded.bytes)).toEqual([0x90, 0xCD, 0x21, 0xF4]);

    const decoded = decoder.decode(encoded.bytes);
    expect(decoded).toContain('NOP');
    expect(decoded).toContain('INT');
    expect(decoded).toContain('HLT');
  });
});
