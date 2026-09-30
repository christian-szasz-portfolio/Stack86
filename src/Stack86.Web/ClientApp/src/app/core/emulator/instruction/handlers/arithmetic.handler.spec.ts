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
import { ArithmeticHandler } from './arithmetic.handler';

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

describe('ArithmeticHandler.mov', () => {
  it('writes immediate to register', () => {
    const c = ctx();
    ArithmeticHandler.mov(c, [reg('ax'), imm(0x1234)]);
    expect(c.cpu.ax).toBe(0x1234);
  });

  it('writes register-to-register', () => {
    const c = ctx();
    c.cpu.bx = 0xCAFE;
    ArithmeticHandler.mov(c, [reg('ax'), reg('bx')]);
    expect(c.cpu.ax).toBe(0xCAFE);
  });

  it('throws when wrong number of operands', () => {
    const c = ctx();
    expect(() => ArithmeticHandler.mov(c, [reg('ax')])).toThrow(/MOV expects 2/);
  });
});

describe('ArithmeticHandler.add (16-bit)', () => {
  it('adds two registers and stores in dst', () => {
    const c = ctx();
    c.cpu.ax = 5;
    c.cpu.bx = 7;
    ArithmeticHandler.add(c, [reg('ax'), reg('bx')]);
    expect(c.cpu.ax).toBe(12);
    expect(c.cpu.flags.zero).toBe(false);
    expect(c.cpu.flags.carry).toBe(false);
    expect(c.cpu.flags.sign).toBe(false);
  });

  it('sets ZF when result is zero', () => {
    const c = ctx();
    c.cpu.ax = 0;
    ArithmeticHandler.add(c, [reg('ax'), imm(0)]);
    expect(c.cpu.flags.zero).toBe(true);
  });

  it('sets CF on unsigned overflow', () => {
    const c = ctx();
    c.cpu.ax = 0xFFFF;
    ArithmeticHandler.add(c, [reg('ax'), imm(1)]);
    expect(c.cpu.ax).toBe(0);
    expect(c.cpu.flags.carry).toBe(true);
    expect(c.cpu.flags.zero).toBe(true);
  });

  it('sets SF for negative signed result', () => {
    const c = ctx();
    c.cpu.ax = 0;
    ArithmeticHandler.add(c, [reg('ax'), imm(0x8000)]);
    expect(c.cpu.flags.sign).toBe(true);
  });
});

describe('ArithmeticHandler.add (8-bit)', () => {
  it('adds and wraps within 8 bits', () => {
    const c = ctx();
    c.cpu.ax = 0xAA00 | 0xFF; // AL = 0xFF
    ArithmeticHandler.add(c, [reg('al'), imm(1)]);
    expect(c.cpu.ax & 0xFF).toBe(0);
    expect(c.cpu.flags.carry).toBe(true);
    expect(c.cpu.flags.zero).toBe(true);
    expect(c.cpu.ax >> 8).toBe(0xAA); // AH preserved
  });
});

describe('ArithmeticHandler.sub', () => {
  it('subtracts two registers', () => {
    const c = ctx();
    c.cpu.ax = 10;
    c.cpu.bx = 3;
    ArithmeticHandler.sub(c, [reg('ax'), reg('bx')]);
    expect(c.cpu.ax).toBe(7);
  });

  it('sets ZF when result is zero', () => {
    const c = ctx();
    c.cpu.ax = 5;
    ArithmeticHandler.sub(c, [reg('ax'), imm(5)]);
    expect(c.cpu.ax).toBe(0);
    expect(c.cpu.flags.zero).toBe(true);
  });

  it('sets CF when borrow occurs', () => {
    const c = ctx();
    c.cpu.ax = 1;
    ArithmeticHandler.sub(c, [reg('ax'), imm(2)]);
    expect(c.cpu.ax).toBe(0xFFFF);
    expect(c.cpu.flags.carry).toBe(true);
    expect(c.cpu.flags.sign).toBe(true);
  });
});

describe('ArithmeticHandler.cmp', () => {
  it('updates flags but does not store', () => {
    const c = ctx();
    c.cpu.ax = 5;
    ArithmeticHandler.cmp(c, [reg('ax'), imm(5)]);
    expect(c.cpu.ax).toBe(5);
    expect(c.cpu.flags.zero).toBe(true);
  });

  it('sets carry flag when first operand is smaller', () => {
    const c = ctx();
    c.cpu.ax = 1;
    ArithmeticHandler.cmp(c, [reg('ax'), imm(2)]);
    expect(c.cpu.flags.carry).toBe(true);
    expect(c.cpu.flags.zero).toBe(false);
  });
});

describe('ArithmeticHandler.inc / dec', () => {
  it('inc increments and preserves carry flag', () => {
    const c = ctx();
    c.cpu.ax = 5;
    c.cpu.flags.carry = true;
    ArithmeticHandler.inc(c, [reg('ax')]);
    expect(c.cpu.ax).toBe(6);
    expect(c.cpu.flags.carry).toBe(true); // INC preserves CF
  });

  it('dec decrements and preserves carry flag', () => {
    const c = ctx();
    c.cpu.ax = 5;
    c.cpu.flags.carry = false;
    ArithmeticHandler.dec(c, [reg('ax')]);
    expect(c.cpu.ax).toBe(4);
    expect(c.cpu.flags.carry).toBe(false);
  });

  it('inc sets ZF when wrapping to zero', () => {
    const c = ctx();
    c.cpu.ax = 0xFFFF;
    ArithmeticHandler.inc(c, [reg('ax')]);
    expect(c.cpu.ax).toBe(0);
    expect(c.cpu.flags.zero).toBe(true);
  });

  it('dec sets ZF when reaching zero', () => {
    const c = ctx();
    c.cpu.ax = 1;
    ArithmeticHandler.dec(c, [reg('ax')]);
    expect(c.cpu.ax).toBe(0);
    expect(c.cpu.flags.zero).toBe(true);
  });
});

describe('ArithmeticHandler.mul (16-bit)', () => {
  it('multiplies AX by operand, splitting result into DX:AX', () => {
    const c = ctx();
    c.cpu.ax = 0x1000;
    c.cpu.bx = 0x10;
    ArithmeticHandler.mul(c, [reg('bx')]);
    expect(c.cpu.ax).toBe(0x0000);
    expect(c.cpu.dx).toBe(0x0001);
    expect(c.cpu.flags.carry).toBe(true);
    expect(c.cpu.flags.overflow).toBe(true);
  });

  it('clears CF/OF when product fits in AX', () => {
    const c = ctx();
    c.cpu.ax = 3;
    c.cpu.bx = 4;
    ArithmeticHandler.mul(c, [reg('bx')]);
    expect(c.cpu.ax).toBe(12);
    expect(c.cpu.dx).toBe(0);
    expect(c.cpu.flags.carry).toBe(false);
  });
});

describe('ArithmeticHandler.mul (8-bit)', () => {
  it('multiplies AL by operand into AX', () => {
    const c = ctx();
    c.cpu.ax = 0x10; // AL=0x10
    c.cpu.bx = 0x20; // BL=0x20
    ArithmeticHandler.mul(c, [reg('bl')]);
    expect(c.cpu.ax).toBe(0x200);
    expect(c.cpu.flags.carry).toBe(true);
  });
});

describe('ArithmeticHandler.div', () => {
  it('throws on divide-by-zero', () => {
    const c = ctx();
    c.cpu.ax = 10;
    c.cpu.bx = 0;
    expect(() => ArithmeticHandler.div(c, [reg('bx')])).toThrow(/zero/i);
  });

  it('divides AX/BX into AX (quotient) DX (remainder)', () => {
    const c = ctx();
    c.cpu.dx = 0;
    c.cpu.ax = 17;
    c.cpu.bx = 5;
    ArithmeticHandler.div(c, [reg('bx')]);
    expect(c.cpu.ax).toBe(3);
    expect(c.cpu.dx).toBe(2);
  });

  it('uses DX:AX as 32-bit dividend', () => {
    const c = ctx();
    c.cpu.dx = 0x0001;
    c.cpu.ax = 0x0000; // 0x10000 = 65536
    c.cpu.bx = 0x100; // 256
    ArithmeticHandler.div(c, [reg('bx')]);
    expect(c.cpu.ax).toBe(0x100); // 65536 / 256 = 256
    expect(c.cpu.dx).toBe(0);
  });

  it('byte div: AX / r8 → AL=quot, AH=rem', () => {
    const c = ctx();
    c.cpu.ax = 17;
    c.cpu.bx = 5;
    ArithmeticHandler.div(c, [reg('bl')]);
    expect(c.cpu.ax & 0xFF).toBe(3);
    expect((c.cpu.ax >> 8) & 0xFF).toBe(2);
  });
});

describe('ArithmeticHandler.neg', () => {
  it('two\'s complement of a positive value', () => {
    const c = ctx();
    c.cpu.ax = 5;
    ArithmeticHandler.neg(c, [reg('ax')]);
    expect(c.cpu.ax).toBe(0xFFFB);
    expect(c.cpu.flags.carry).toBe(true); // NEG sets CF for non-zero
    expect(c.cpu.flags.sign).toBe(true);
  });

  it('NEG of zero leaves zero and clears CF', () => {
    const c = ctx();
    c.cpu.ax = 0;
    ArithmeticHandler.neg(c, [reg('ax')]);
    expect(c.cpu.ax).toBe(0);
    expect(c.cpu.flags.zero).toBe(true);
    expect(c.cpu.flags.carry).toBe(false);
  });
});
