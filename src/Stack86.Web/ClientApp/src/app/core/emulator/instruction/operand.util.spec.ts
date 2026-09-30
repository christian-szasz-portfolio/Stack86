import { describe, expect, it } from 'vitest';
import { CpuHelper } from '../cpu/cpu.model';
import { Memory } from '../memory/memory.model';
import { OperandHelper } from './operand.util';
import {
  ExecutionContext,
  Operand,
  OperandSize,
  OperandType,
} from './instruction.model';
import { HeapAllocator } from '../memory/heap-allocator';

function ctx(): ExecutionContext {
  const cpu = CpuHelper.createInitialState();
  const memory = new Memory();
  return {
    cpu,
    memory,
    labels: new Map<string, number>(),
    halted: false,
    heap: new HeapAllocator(memory),
  };
}

function reg(name: string, size?: OperandSize): Operand {
  return { type: OperandType.Register, value: name, size };
}

function imm(value: number): Operand {
  return { type: OperandType.Immediate, value };
}

function mem(value: string | number, size: OperandSize, offset?: number): Operand {
  return { type: OperandType.Memory, value, size, offset };
}

function label(name: string): Operand {
  return { type: OperandType.Label, value: name };
}

describe('OperandHelper.read', () => {
  it('reads immediate numeric operand', () => {
    const c = ctx();
    expect(OperandHelper.read(c.cpu, c.memory, imm(42), c.labels)).toBe(42);
  });

  it('reads immediate string operand by parseInt', () => {
    const c = ctx();
    const op: Operand = { type: OperandType.Immediate, value: '123' as unknown as string };
    expect(OperandHelper.read(c.cpu, c.memory, op, c.labels)).toBe(123);
  });

  it('reads a 16-bit register', () => {
    const c = ctx();
    c.cpu.ax = 0x1234;
    expect(OperandHelper.read(c.cpu, c.memory, reg('ax'), c.labels)).toBe(0x1234);
  });

  it('reads the high byte of a sub-register', () => {
    const c = ctx();
    c.cpu.ax = 0xABCD;
    expect(OperandHelper.read(c.cpu, c.memory, reg('ah'), c.labels)).toBe(0xAB);
    expect(OperandHelper.read(c.cpu, c.memory, reg('al'), c.labels)).toBe(0xCD);
  });

  it('throws on unknown register', () => {
    const c = ctx();
    expect(() => OperandHelper.read(c.cpu, c.memory, reg('zz'), c.labels)).toThrow();
  });

  it('reads a label address', () => {
    const c = ctx();
    c.labels.set('start', 0x100);
    expect(OperandHelper.read(c.cpu, c.memory, label('start'), c.labels)).toBe(0x100);
  });

  it('returns 0 for the @DATA label even if undefined', () => {
    const c = ctx();
    expect(OperandHelper.read(c.cpu, c.memory, label('@DATA'), c.labels)).toBe(0);
  });

  it('throws for undefined labels (other than @DATA)', () => {
    const c = ctx();
    expect(() => OperandHelper.read(c.cpu, c.memory, label('missing'), c.labels)).toThrow();
  });

  it('reads a word from a memory operand at a numeric address', () => {
    const c = ctx();
    c.memory.writeWord(0x200, 0xBEEF);
    expect(OperandHelper.read(c.cpu, c.memory, mem(0x200, OperandSize.Word), c.labels)).toBe(0xBEEF);
  });

  it('reads a byte from a memory operand at a numeric address', () => {
    const c = ctx();
    c.memory.writeByte(0x200, 0x7F);
    expect(OperandHelper.read(c.cpu, c.memory, mem(0x200, OperandSize.Byte), c.labels)).toBe(0x7F);
  });

  it('resolves memory address through a register', () => {
    const c = ctx();
    c.cpu.bx = 0x300;
    c.memory.writeWord(0x300, 0xCAFE);
    expect(OperandHelper.read(c.cpu, c.memory, mem('bx', OperandSize.Word), c.labels)).toBe(0xCAFE);
  });

  it('applies offset to memory address', () => {
    const c = ctx();
    c.cpu.bx = 0x300;
    c.memory.writeWord(0x302, 0xDEAD);
    expect(OperandHelper.read(c.cpu, c.memory, mem('bx', OperandSize.Word, 2), c.labels)).toBe(0xDEAD);
  });

  it('resolves memory address through a label', () => {
    const c = ctx();
    c.labels.set('msg', 0x400);
    c.memory.writeWord(0x400, 0x1111);
    expect(OperandHelper.read(c.cpu, c.memory, mem('msg', OperandSize.Word), c.labels)).toBe(0x1111);
  });

  it('falls back to hex parsing for raw memory address strings', () => {
    const c = ctx();
    c.memory.writeWord(0x200, 0x4242);
    expect(OperandHelper.read(c.cpu, c.memory, mem('200', OperandSize.Word), c.labels)).toBe(0x4242);
  });

  it('throws when memory address cannot be resolved', () => {
    const c = ctx();
    expect(() => OperandHelper.read(c.cpu, c.memory, mem('nope', OperandSize.Word), c.labels)).toThrow();
  });
});

describe('OperandHelper.write', () => {
  it('writes a 16-bit register, masking high bits', () => {
    const c = ctx();
    OperandHelper.write(c.cpu, c.memory, reg('ax'), 0x1FFFF, c.labels);
    expect(c.cpu.ax).toBe(0xFFFF);
  });

  it('writes a high sub-register without disturbing the low byte', () => {
    const c = ctx();
    c.cpu.ax = 0x00FF;
    OperandHelper.write(c.cpu, c.memory, reg('ah'), 0xAA, c.labels);
    expect(c.cpu.ax).toBe(0xAAFF);
  });

  it('writes a low sub-register without disturbing the high byte', () => {
    const c = ctx();
    c.cpu.ax = 0xAA00;
    OperandHelper.write(c.cpu, c.memory, reg('al'), 0xBB, c.labels);
    expect(c.cpu.ax).toBe(0xAABB);
  });

  it('throws when writing to an unknown register', () => {
    const c = ctx();
    expect(() => OperandHelper.write(c.cpu, c.memory, reg('zz'), 1, c.labels)).toThrow();
  });

  it('writes a word memory operand', () => {
    const c = ctx();
    OperandHelper.write(c.cpu, c.memory, mem(0x200, OperandSize.Word), 0xBEEF, c.labels);
    expect(c.memory.readWord(0x200)).toBe(0xBEEF);
  });

  it('writes a byte memory operand', () => {
    const c = ctx();
    OperandHelper.write(c.cpu, c.memory, mem(0x200, OperandSize.Byte), 0x12, c.labels);
    expect(c.memory.readByte(0x200)).toBe(0x12);
  });

  it('throws when writing to an immediate operand', () => {
    const c = ctx();
    expect(() => OperandHelper.write(c.cpu, c.memory, imm(1), 5, c.labels)).toThrow();
  });

  it('throws when writing to a label operand', () => {
    const c = ctx();
    c.labels.set('here', 0x100);
    expect(() => OperandHelper.write(c.cpu, c.memory, label('here'), 5, c.labels)).toThrow();
  });
});

describe('OperandHelper.size', () => {
  it('returns the explicit size when provided', () => {
    expect(OperandHelper.size({ type: OperandType.Memory, value: 0x100, size: OperandSize.Byte })).toBe(OperandSize.Byte);
  });

  it('infers byte size for sub-registers', () => {
    expect(OperandHelper.size(reg('al'))).toBe(OperandSize.Byte);
    expect(OperandHelper.size(reg('bh'))).toBe(OperandSize.Byte);
  });

  it('infers word size for full registers', () => {
    expect(OperandHelper.size(reg('ax'))).toBe(OperandSize.Word);
    expect(OperandHelper.size(reg('cx'))).toBe(OperandSize.Word);
  });

  it('defaults to word for non-register operands without explicit size', () => {
    expect(OperandHelper.size(imm(5))).toBe(OperandSize.Word);
  });
});
