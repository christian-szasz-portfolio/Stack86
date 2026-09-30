import { ExecutionContext, Operand, OperandSize } from '../instruction.model';
import { OperandHelper } from '../operand.util';
import { FlagHelper } from '../flags.util';
import { CpuHelper } from '../../cpu/cpu.model';

export class ArithmeticHandler {
  public static mov(ctx: ExecutionContext, ops: Operand[]): void {
    ArithmeticHandler.assertOperandCount('MOV', ops, 2);
    const value = OperandHelper.read(ctx.cpu, ctx.memory, ops[1], ctx.labels, ctx.trace);
    OperandHelper.write(ctx.cpu, ctx.memory, ops[0], value, ctx.labels, ctx.trace);
  }

  public static add(ctx: ExecutionContext, ops: Operand[]): void {
    ArithmeticHandler.assertOperandCount('ADD', ops, 2);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const b = OperandHelper.read(ctx.cpu, ctx.memory, ops[1], ctx.labels, ctx.trace);
    const size = OperandHelper.size(ops[0]);
    if (size === OperandSize.Byte) {
      const signed = CpuHelper.toSigned8(a) + CpuHelper.toSigned8(b);
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toByte(signed), ctx.labels, ctx.trace);
      FlagHelper.updateArithmetic8(ctx.cpu, signed, (a + b) > 0xFF);
    } else {
      const signed = CpuHelper.toSigned16(a) + CpuHelper.toSigned16(b);
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toWord(signed), ctx.labels, ctx.trace);
      FlagHelper.updateArithmetic16(ctx.cpu, signed, (a + b) > 0xFFFF);
    }
  }

  public static sub(ctx: ExecutionContext, ops: Operand[]): void {
    ArithmeticHandler.assertOperandCount('SUB', ops, 2);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const b = OperandHelper.read(ctx.cpu, ctx.memory, ops[1], ctx.labels, ctx.trace);
    const size = OperandHelper.size(ops[0]);
    if (size === OperandSize.Byte) {
      const signed = CpuHelper.toSigned8(a) - CpuHelper.toSigned8(b);
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toByte(signed), ctx.labels, ctx.trace);
      FlagHelper.updateArithmetic8(ctx.cpu, signed, a < b);
    } else {
      const signed = CpuHelper.toSigned16(a) - CpuHelper.toSigned16(b);
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toWord(signed), ctx.labels, ctx.trace);
      FlagHelper.updateArithmetic16(ctx.cpu, signed, a < b);
    }
  }

  public static cmp(ctx: ExecutionContext, ops: Operand[]): void {
    ArithmeticHandler.assertOperandCount('CMP', ops, 2);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const b = OperandHelper.read(ctx.cpu, ctx.memory, ops[1], ctx.labels, ctx.trace);
    const size = OperandHelper.size(ops[0]);
    if (size === OperandSize.Byte) {
      FlagHelper.updateArithmetic8(ctx.cpu, CpuHelper.toSigned8(a) - CpuHelper.toSigned8(b), a < b);
    } else {
      FlagHelper.updateArithmetic16(ctx.cpu, CpuHelper.toSigned16(a) - CpuHelper.toSigned16(b), a < b);
    }
  }

  public static inc(ctx: ExecutionContext, ops: Operand[]): void {
    ArithmeticHandler.assertOperandCount('INC', ops, 1);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const size = OperandHelper.size(ops[0]);
    const oldCarry = ctx.cpu.flags.carry;
    if (size === OperandSize.Byte) {
      const signed = CpuHelper.toSigned8(a) + 1;
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toByte(signed), ctx.labels, ctx.trace);
      FlagHelper.updateArithmetic8(ctx.cpu, signed, false);
    } else {
      const signed = CpuHelper.toSigned16(a) + 1;
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toWord(signed), ctx.labels, ctx.trace);
      FlagHelper.updateArithmetic16(ctx.cpu, signed, false);
    }
    ctx.cpu.flags.carry = oldCarry;
  }

  public static dec(ctx: ExecutionContext, ops: Operand[]): void {
    ArithmeticHandler.assertOperandCount('DEC', ops, 1);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const size = OperandHelper.size(ops[0]);
    const oldCarry = ctx.cpu.flags.carry;
    if (size === OperandSize.Byte) {
      const signed = CpuHelper.toSigned8(a) - 1;
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toByte(signed), ctx.labels, ctx.trace);
      FlagHelper.updateArithmetic8(ctx.cpu, signed, false);
    } else {
      const signed = CpuHelper.toSigned16(a) - 1;
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toWord(signed), ctx.labels, ctx.trace);
      FlagHelper.updateArithmetic16(ctx.cpu, signed, false);
    }
    ctx.cpu.flags.carry = oldCarry;
  }

  public static mul(ctx: ExecutionContext, ops: Operand[]): void {
    ArithmeticHandler.assertOperandCount('MUL', ops, 1);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const b = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const size = OperandHelper.size(ops[0]);
    if (size === OperandSize.Byte) {
      const result = (ctx.cpu.ax & 0xFF) * b;
      ctx.cpu.ax = CpuHelper.toWord(result);
      ctx.cpu.flags.carry = ctx.cpu.flags.overflow = result > 0xFF;
    } else {
      const result = ctx.cpu.ax * b;
      ctx.cpu.ax = CpuHelper.toWord(result & 0xFFFF);
      ctx.cpu.dx = CpuHelper.toWord((result >> 16) & 0xFFFF);
      ctx.cpu.flags.carry = ctx.cpu.flags.overflow = result > 0xFFFF;
    }
  }

  public static div(ctx: ExecutionContext, ops: Operand[]): void {
    ArithmeticHandler.assertOperandCount('DIV', ops, 1);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const b = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    if (b === 0) throw new Error('Division by zero');
    const size = OperandHelper.size(ops[0]);
    if (size === OperandSize.Byte) {
      const dividend = ctx.cpu.ax;
      ctx.cpu.ax = CpuHelper.toWord(((dividend / b) & 0xFF) | (((dividend % b) & 0xFF) << 8));
    } else {
      const dividend = (ctx.cpu.dx << 16) | ctx.cpu.ax;
      ctx.cpu.ax = CpuHelper.toWord(Math.floor(dividend / b));
      ctx.cpu.dx = CpuHelper.toWord(dividend % b);
    }
  }

  public static neg(ctx: ExecutionContext, ops: Operand[]): void {
    ArithmeticHandler.assertOperandCount('NEG', ops, 1);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const size = OperandHelper.size(ops[0]);
    if (size === OperandSize.Byte) {
      const signed = -CpuHelper.toSigned8(a);
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toByte(signed), ctx.labels, ctx.trace);
      FlagHelper.updateArithmetic8(ctx.cpu, signed, a !== 0);
    } else {
      const signed = -CpuHelper.toSigned16(a);
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toWord(signed), ctx.labels, ctx.trace);
      FlagHelper.updateArithmetic16(ctx.cpu, signed, a !== 0);
    }
  }

  private static assertOperandCount(mnemonic: string, ops: Operand[], expected: number): void {
    if (ops.length !== expected) {
      throw new Error(`${mnemonic} expects ${expected} operand(s), got ${ops.length}`);
    }
  }
}
