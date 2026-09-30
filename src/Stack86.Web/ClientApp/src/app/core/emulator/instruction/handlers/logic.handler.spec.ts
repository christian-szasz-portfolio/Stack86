import { describe, expect, it } from 'vitest';
import { CpuHelper } from '../../cpu/cpu.model';
import { Memory } from '../../memory/memory.model';
import { HeapAllocator } from '../../memory/heap-allocator';
import {
  ExecutionContext,
  Operand,
  OperandSize,
  OperandType,
} from '../instruction.model';
import { LogicHandler } from './logic.handler';

function ctx(): ExecutionContext {
  const cpu = CpuHelper.createInitialState();
  const memory = new Memory();
  return { cpu, memory, labels: new Map(), halted: false, heap: new HeapAllocator(memory) };
}

function reg(name: string, size?: OperandSize): Operand {
  return { type: OperandType.Register, value: name, size };
}

function imm(value: number): Operand {
  return { type: OperandType.Immediate, value };
}

describe('LogicHandler.and', () => {
  it('bitwise ANDs and stores in dst', () => {
    const c = ctx();
    c.cpu.ax = 0xF0F0;
    LogicHandler.and(c, [reg('ax'), imm(0x0FF0)]);
    expect(c.cpu.ax).toBe(0x00F0);
  });

  it('sets ZF when result is zero', () => {
    const c = ctx();
    c.cpu.ax = 0xFF00;
    LogicHandler.and(c, [reg('ax'), imm(0x00FF)]);
    expect(c.cpu.flags.zero).toBe(true);
  });

  it('sets SF for negative result', () => {
    const c = ctx();
    c.cpu.ax = 0xFFFF;
    LogicHandler.and(c, [reg('ax'), imm(0x8000)]);
    expect(c.cpu.flags.sign).toBe(true);
  });

  it('rejects mismatched operand counts', () => {
    expect(() => LogicHandler.and(ctx(), [reg('ax')])).toThrow(/AND expects 2/);
  });
});

describe('LogicHandler.or', () => {
  it('bitwise ORs', () => {
    const c = ctx();
    c.cpu.ax = 0x00F0;
    LogicHandler.or(c, [reg('ax'), imm(0x0F0F)]);
    expect(c.cpu.ax).toBe(0x0FFF);
  });

  it('OR with 0 leaves value unchanged but updates flags', () => {
    const c = ctx();
    c.cpu.ax = 0;
    LogicHandler.or(c, [reg('ax'), imm(0)]);
    expect(c.cpu.flags.zero).toBe(true);
  });
});

describe('LogicHandler.xor', () => {
  it('xor with self clears the register', () => {
    const c = ctx();
    c.cpu.ax = 0x1234;
    LogicHandler.xor(c, [reg('ax'), reg('ax')]);
    expect(c.cpu.ax).toBe(0);
    expect(c.cpu.flags.zero).toBe(true);
  });

  it('toggles individual bits', () => {
    const c = ctx();
    c.cpu.ax = 0x00FF;
    LogicHandler.xor(c, [reg('ax'), imm(0xFFFF)]);
    expect(c.cpu.ax).toBe(0xFF00);
  });
});

describe('LogicHandler.not', () => {
  it('inverts all bits (16-bit)', () => {
    const c = ctx();
    c.cpu.ax = 0x00FF;
    LogicHandler.not(c, [reg('ax')]);
    expect(c.cpu.ax).toBe(0xFF00);
  });

  it('inverts a sub-register byte', () => {
    const c = ctx();
    c.cpu.ax = 0xAA00; // AL = 0
    LogicHandler.not(c, [reg('al')]);
    expect(c.cpu.ax & 0xFF).toBe(0xFF);
    expect((c.cpu.ax >> 8) & 0xFF).toBe(0xAA); // AH preserved
  });

  it('rejects wrong operand count', () => {
    expect(() => LogicHandler.not(ctx(), [reg('ax'), reg('bx')])).toThrow(/NOT expects 1/);
  });
});

describe('LogicHandler.shl', () => {
  it('shifts left by an immediate count', () => {
    const c = ctx();
    c.cpu.ax = 0x0001;
    LogicHandler.shl(c, [reg('ax'), imm(4)]);
    expect(c.cpu.ax).toBe(0x0010);
  });

  it('zeros out high bits when shifted past 16', () => {
    const c = ctx();
    c.cpu.ax = 0x0001;
    LogicHandler.shl(c, [reg('ax'), imm(16)]);
    expect(c.cpu.ax).toBe(0);
    expect(c.cpu.flags.zero).toBe(true);
  });

  it('byte shift wraps within 8 bits', () => {
    const c = ctx();
    c.cpu.ax = 0xAA01;
    LogicHandler.shl(c, [reg('al'), imm(4)]);
    expect(c.cpu.ax & 0xFF).toBe(0x10);
    expect((c.cpu.ax >> 8) & 0xFF).toBe(0xAA);
  });
});

describe('LogicHandler.shr', () => {
  it('logical shift right zero-extends', () => {
    const c = ctx();
    c.cpu.ax = 0x8000;
    LogicHandler.shr(c, [reg('ax'), imm(1)]);
    expect(c.cpu.ax).toBe(0x4000);
  });

  it('shifts right by CL-style register count', () => {
    const c = ctx();
    c.cpu.ax = 0xFF00;
    c.cpu.cx = 4;
    LogicHandler.shr(c, [reg('ax'), reg('cx')]);
    expect(c.cpu.ax).toBe(0x0FF0);
  });

  it('sets ZF when result becomes zero', () => {
    const c = ctx();
    c.cpu.ax = 1;
    LogicHandler.shr(c, [reg('ax'), imm(1)]);
    expect(c.cpu.flags.zero).toBe(true);
  });
});
