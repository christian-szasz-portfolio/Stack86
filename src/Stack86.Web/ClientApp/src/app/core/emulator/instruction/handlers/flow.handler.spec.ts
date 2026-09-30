import { describe, expect, it } from 'vitest';
import { CpuHelper } from '../../cpu/cpu.model';
import { Memory } from '../../memory/memory.model';
import { HeapAllocator } from '../../memory/heap-allocator';
import {
  ExecutionContext,
  Operand,
  OperandType,
} from '../instruction.model';
import { FlowHandler } from './flow.handler';

function ctx(ip: number = 0x100): ExecutionContext {
  const cpu = CpuHelper.createInitialState();
  cpu.ip = ip;
  cpu.sp = 0xFFFE;
  const memory = new Memory();
  return {
    cpu,
    memory,
    labels: new Map<string, number>([['target', 0x200]]),
    halted: false,
    heap: new HeapAllocator(memory),
  };
}

function label(name: string): Operand {
  return { type: OperandType.Label, value: name };
}

function imm(value: number): Operand {
  return { type: OperandType.Immediate, value };
}

describe('FlowHandler.jmp', () => {
  it('unconditionally sets IP to target', () => {
    const c = ctx(0x100);
    FlowHandler.jmp(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('throws when no operands are provided', () => {
    expect(() => FlowHandler.jmp(ctx(), [])).toThrow();
  });
});

describe('FlowHandler conditional jumps', () => {
  it('JE jumps when ZF=1', () => {
    const c = ctx();
    c.cpu.flags.zero = true;
    FlowHandler.je(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('JE does not jump when ZF=0', () => {
    const c = ctx();
    c.cpu.flags.zero = false;
    FlowHandler.je(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x100);
  });

  it('JNE jumps when ZF=0', () => {
    const c = ctx();
    c.cpu.flags.zero = false;
    FlowHandler.jne(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('JG jumps when ZF=0 and SF=OF', () => {
    const c = ctx();
    c.cpu.flags.zero = false;
    c.cpu.flags.sign = true;
    c.cpu.flags.overflow = true;
    FlowHandler.jg(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('JG does not jump when ZF=1', () => {
    const c = ctx();
    c.cpu.flags.zero = true;
    c.cpu.flags.sign = true;
    c.cpu.flags.overflow = true;
    FlowHandler.jg(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x100);
  });

  it('JGE jumps when SF=OF (regardless of ZF)', () => {
    const c = ctx();
    c.cpu.flags.zero = true;
    c.cpu.flags.sign = false;
    c.cpu.flags.overflow = false;
    FlowHandler.jge(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('JL jumps when SF != OF', () => {
    const c = ctx();
    c.cpu.flags.sign = true;
    c.cpu.flags.overflow = false;
    FlowHandler.jl(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('JLE jumps when ZF=1 OR SF != OF', () => {
    const c = ctx();
    c.cpu.flags.zero = true;
    FlowHandler.jle(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('JA jumps when CF=0 and ZF=0', () => {
    const c = ctx();
    FlowHandler.ja(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('JA does not jump when CF=1', () => {
    const c = ctx();
    c.cpu.flags.carry = true;
    FlowHandler.ja(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x100);
  });

  it('JB jumps when CF=1', () => {
    const c = ctx();
    c.cpu.flags.carry = true;
    FlowHandler.jb(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('JC is an alias for JB', () => {
    const c = ctx();
    c.cpu.flags.carry = true;
    FlowHandler.jc(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('JZ is an alias for JE', () => {
    const c = ctx();
    c.cpu.flags.zero = true;
    FlowHandler.jz(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('JNZ is an alias for JNE', () => {
    const c = ctx();
    c.cpu.flags.zero = false;
    FlowHandler.jnz(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
  });
});

describe('FlowHandler.call / ret', () => {
  it('CALL pushes the current IP and jumps to target', () => {
    const c = ctx(0x150);
    const sp0 = c.cpu.sp;
    FlowHandler.call(c, [label('target')]);
    expect(c.cpu.ip).toBe(0x200);
    expect(c.cpu.sp).toBe(sp0 - 2);
    expect(c.memory.readWord(c.cpu.sp)).toBe(0x150);
  });

  it('RET pops IP and restores SP', () => {
    const c = ctx(0x150);
    FlowHandler.call(c, [label('target')]);
    FlowHandler.ret(c);
    expect(c.cpu.ip).toBe(0x150);
    expect(c.cpu.sp).toBe(0xFFFE);
  });

  it('RET wraps SP back to 0xFFFE through 16-bit math', () => {
    const c = ctx();
    c.cpu.sp = 0xFFFC;
    c.memory.writeWord(0xFFFC, 0x300);
    FlowHandler.ret(c);
    expect(c.cpu.ip).toBe(0x300);
    expect(c.cpu.sp).toBe(0xFFFE);
  });
});

describe('FlowHandler.loop', () => {
  it('decrements CX and jumps when CX != 0', () => {
    const c = ctx();
    c.cpu.cx = 3;
    FlowHandler.loop(c, [label('target')]);
    expect(c.cpu.cx).toBe(2);
    expect(c.cpu.ip).toBe(0x200);
  });

  it('falls through when CX hits zero', () => {
    const c = ctx(0x100);
    c.cpu.cx = 1;
    FlowHandler.loop(c, [label('target')]);
    expect(c.cpu.cx).toBe(0);
    expect(c.cpu.ip).toBe(0x100);
  });

  it('CX wraps from 0 to 0xFFFF and still loops', () => {
    const c = ctx();
    c.cpu.cx = 0;
    FlowHandler.loop(c, [label('target')]);
    expect(c.cpu.cx).toBe(0xFFFF);
    expect(c.cpu.ip).toBe(0x200);
  });
});

describe('FlowHandler.resolveTarget', () => {
  it('accepts an immediate as the jump target', () => {
    const c = ctx();
    FlowHandler.jmp(c, [imm(0x300)]);
    expect(c.cpu.ip).toBe(0x300);
  });
});
