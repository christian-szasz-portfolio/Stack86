import { ExecutionContext, Operand, OperandSize } from '../instruction.model';
import { OperandHelper } from '../operand.util';
import { FlagHelper } from '../flags.util';
import { CpuHelper } from '../../cpu/cpu.model';

export class LogicHandler {
  public static and(ctx: ExecutionContext, ops: Operand[]): void {
    LogicHandler.assertOperandCount('AND', ops, 2);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const b = OperandHelper.read(ctx.cpu, ctx.memory, ops[1], ctx.labels, ctx.trace);
    const result = a & b;
    const size = OperandHelper.size(ops[0]);
    if (size === OperandSize.Byte) {
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toByte(result), ctx.labels, ctx.trace);
      FlagHelper.updateLogic8(ctx.cpu, result);
    } else {
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toWord(result), ctx.labels, ctx.trace);
      FlagHelper.updateLogic16(ctx.cpu, result);
    }
  }

  public static or(ctx: ExecutionContext, ops: Operand[]): void {
    LogicHandler.assertOperandCount('OR', ops, 2);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const b = OperandHelper.read(ctx.cpu, ctx.memory, ops[1], ctx.labels, ctx.trace);
    const result = a | b;
    const size = OperandHelper.size(ops[0]);
    if (size === OperandSize.Byte) {
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toByte(result), ctx.labels, ctx.trace);
      FlagHelper.updateLogic8(ctx.cpu, result);
    } else {
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toWord(result), ctx.labels, ctx.trace);
      FlagHelper.updateLogic16(ctx.cpu, result);
    }
  }

  public static xor(ctx: ExecutionContext, ops: Operand[]): void {
    LogicHandler.assertOperandCount('XOR', ops, 2);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const b = OperandHelper.read(ctx.cpu, ctx.memory, ops[1], ctx.labels, ctx.trace);
    const result = a ^ b;
    const size = OperandHelper.size(ops[0]);
    if (size === OperandSize.Byte) {
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toByte(result), ctx.labels, ctx.trace);
      FlagHelper.updateLogic8(ctx.cpu, result);
    } else {
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toWord(result), ctx.labels, ctx.trace);
      FlagHelper.updateLogic16(ctx.cpu, result);
    }
  }

  public static not(ctx: ExecutionContext, ops: Operand[]): void {
    LogicHandler.assertOperandCount('NOT', ops, 1);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const size = OperandHelper.size(ops[0]);
    const result = size === OperandSize.Byte ? CpuHelper.toByte(~a) : CpuHelper.toWord(~a);
    OperandHelper.write(ctx.cpu, ctx.memory, ops[0], result, ctx.labels, ctx.trace);
  }

  public static shl(ctx: ExecutionContext, ops: Operand[]): void {
    LogicHandler.assertOperandCount('SHL', ops, 2);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const count = OperandHelper.read(ctx.cpu, ctx.memory, ops[1], ctx.labels, ctx.trace);
    const size = OperandHelper.size(ops[0]);
    const result = a << count;
    if (size === OperandSize.Byte) {
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toByte(result), ctx.labels, ctx.trace);
      FlagHelper.updateLogic8(ctx.cpu, result);
    } else {
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toWord(result), ctx.labels, ctx.trace);
      FlagHelper.updateLogic16(ctx.cpu, result);
    }
  }

  public static shr(ctx: ExecutionContext, ops: Operand[]): void {
    LogicHandler.assertOperandCount('SHR', ops, 2);
    if (ctx.trace) ctx.trace.aluUsed = true;
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const count = OperandHelper.read(ctx.cpu, ctx.memory, ops[1], ctx.labels, ctx.trace);
    const result = a >>> count;
    const size = OperandHelper.size(ops[0]);
    if (size === OperandSize.Byte) {
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toByte(result), ctx.labels, ctx.trace);
      FlagHelper.updateLogic8(ctx.cpu, result);
    } else {
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toWord(result), ctx.labels, ctx.trace);
      FlagHelper.updateLogic16(ctx.cpu, result);
    }
  }

  private static assertOperandCount(mnemonic: string, ops: Operand[], expected: number): void {
    if (ops.length !== expected) {
      throw new Error(`${mnemonic} expects ${expected} operand(s), got ${ops.length}`);
    }
  }
}
