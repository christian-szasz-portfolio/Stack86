import { ExecutionContext, Operand } from '../instruction.model';
import { OperandHelper } from '../operand.util';
import { CpuHelper } from '../../cpu/cpu.model';
import { StackAccessType } from '../../execution/execution-trace.model';

export class FlowHandler {
  public static jmp(ctx: ExecutionContext, ops: Operand[]): void {
    ctx.cpu.ip = FlowHandler.resolveTarget(ctx, ops);
  }

  public static je(ctx: ExecutionContext, ops: Operand[]): void {
    if (ctx.cpu.flags.zero) ctx.cpu.ip = FlowHandler.resolveTarget(ctx, ops);
  }

  public static jne(ctx: ExecutionContext, ops: Operand[]): void {
    if (!ctx.cpu.flags.zero) ctx.cpu.ip = FlowHandler.resolveTarget(ctx, ops);
  }

  public static jg(ctx: ExecutionContext, ops: Operand[]): void {
    if (!ctx.cpu.flags.zero && ctx.cpu.flags.sign === ctx.cpu.flags.overflow)
      ctx.cpu.ip = FlowHandler.resolveTarget(ctx, ops);
  }

  public static jge(ctx: ExecutionContext, ops: Operand[]): void {
    if (ctx.cpu.flags.sign === ctx.cpu.flags.overflow)
      ctx.cpu.ip = FlowHandler.resolveTarget(ctx, ops);
  }

  public static jl(ctx: ExecutionContext, ops: Operand[]): void {
    if (ctx.cpu.flags.sign !== ctx.cpu.flags.overflow)
      ctx.cpu.ip = FlowHandler.resolveTarget(ctx, ops);
  }

  public static jle(ctx: ExecutionContext, ops: Operand[]): void {
    if (ctx.cpu.flags.zero || ctx.cpu.flags.sign !== ctx.cpu.flags.overflow)
      ctx.cpu.ip = FlowHandler.resolveTarget(ctx, ops);
  }

  public static ja(ctx: ExecutionContext, ops: Operand[]): void {
    if (!ctx.cpu.flags.carry && !ctx.cpu.flags.zero)
      ctx.cpu.ip = FlowHandler.resolveTarget(ctx, ops);
  }

  public static jb(ctx: ExecutionContext, ops: Operand[]): void {
    if (ctx.cpu.flags.carry) ctx.cpu.ip = FlowHandler.resolveTarget(ctx, ops);
  }

  public static jc(ctx: ExecutionContext, ops: Operand[]): void {
    FlowHandler.jb(ctx, ops);
  }

  public static jz(ctx: ExecutionContext, ops: Operand[]): void {
    FlowHandler.je(ctx, ops);
  }

  public static jnz(ctx: ExecutionContext, ops: Operand[]): void {
    FlowHandler.jne(ctx, ops);
  }

  public static call(ctx: ExecutionContext, ops: Operand[]): void {
    if (ctx.trace) ctx.trace.stackAccess = StackAccessType.Call;
    const target = FlowHandler.resolveTarget(ctx, ops);
    ctx.cpu.sp = CpuHelper.toWord(ctx.cpu.sp - 2);
    ctx.memory.writeWord(ctx.cpu.sp, ctx.cpu.ip);
    ctx.cpu.ip = target;
  }

  public static ret(ctx: ExecutionContext): void {
    if (ctx.trace) ctx.trace.stackAccess = StackAccessType.Ret;
    ctx.cpu.ip = ctx.memory.readWord(ctx.cpu.sp);
    ctx.cpu.sp = CpuHelper.toWord(ctx.cpu.sp + 2);
  }

  public static loop(ctx: ExecutionContext, ops: Operand[]): void {
    ctx.cpu.cx = CpuHelper.toWord(ctx.cpu.cx - 1);
    if (ctx.cpu.cx !== 0) {
      ctx.cpu.ip = FlowHandler.resolveTarget(ctx, ops);
    }
  }

  private static resolveTarget(ctx: ExecutionContext, ops: Operand[]): number {
    if (ops.length !== 1) throw new Error('Jump/call expects 1 operand');
    return OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
  }
}
