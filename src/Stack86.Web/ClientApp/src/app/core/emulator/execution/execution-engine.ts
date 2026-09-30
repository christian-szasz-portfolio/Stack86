import { CpuState, CpuHelper } from '../cpu/cpu.model';
import { Memory } from '../memory/memory.model';
import { HeapAllocator } from '../memory/heap-allocator';
import { ParsedInstruction, ExecutionContext } from '../instruction/instruction.model';
import { InstructionRegistry } from '../instruction/instruction-registry';
import { Parser, ParseResult, ProgramConfig } from '../parser/parser';
import { ExecutionStatus, ExecutionResult } from './execution-result.model';
import { ExecutionTrace, ExecutionTraceFactory } from './execution-trace.model';

export class ExecutionEngine {
  private cpu: CpuState = CpuHelper.createInitialState();
  private memory = new Memory();
  private instructions: ParsedInstruction[] = [];
  private labels = new Map<string, number>();
  private status: ExecutionStatus = ExecutionStatus.Idle;
  private readonly breakpoints = new Set<number>();
  private error: string | null = null;
  private consoleBuffer: string[] = [];
  private consoleFlushMark = 0;
  private readonly registry = new InstructionRegistry();
  private readonly parser = new Parser();
  private runHandle: number | null = null;
  private maxStepsPerRun = 1_000_000;
  private instructionIndex = new Map<number, number>();
  private codeDataEnd = 0;
  private sleepUntil = 0;
  private heap = new HeapAllocator(this.memory);

  public getStatus(): ExecutionStatus {
    return this.status;
  }

  public getCpu(): CpuState {
    return { ...this.cpu, flags: { ...this.cpu.flags } };
  }

  public getMemory(): Memory {
    return this.memory;
  }

  public getInstructions(): readonly ParsedInstruction[] {
    return this.instructions;
  }

  public getLabels(): ReadonlyMap<string, number> {
    return this.labels;
  }

  public getError(): string | null {
    return this.error;
  }

  public getConsoleOutput(): readonly string[] {
    return this.consoleBuffer;
  }

  /**
   * Returns console output accumulated since the last flush and advances the mark.
   * Use this for incremental UI updates (e.g. during sleep pauses).
   */
  public flushConsoleOutput(): string[] {
    const newOutput = this.consoleBuffer.slice(this.consoleFlushMark);
    this.consoleFlushMark = this.consoleBuffer.length;
    return newOutput;
  }

  public getBreakpoints(): ReadonlySet<number> {
    return this.breakpoints;
  }

  /**
   * Returns the remaining sleep time in milliseconds (set by INT 86h sleep service).
   * Resets to 0 after reading so the caller owns the delay scheduling.
   */
  public getSleepRemaining(): number {
    if (this.sleepUntil <= 0) {
      return 0;
    }
    const remaining = Math.max(this.sleepUntil - Date.now(), 0);
    this.sleepUntil = 0;
    return remaining;
  }

  public loadProgram(source: string): ParseResult {
    this.reset();
    const result = this.parser.parse(source);

    if (result.errors.length > 0) {
      this.status = ExecutionStatus.Error;
      this.error = result.errors.map(e => e.toString()).join('\n');
      return result;
    }

    this.labels = result.labels;

    // Configure SP from .MODEL / .STACK directives.
    // Stack grows downward from top of 64KB address space.
    ExecutionEngine.applyConfig(this.cpu, result.config);

    // Separate data directives from executable instructions.
    // DB/DW bytes are written into memory; only real instructions are kept.
    const executable: ParsedInstruction[] = [];
    for (const instr of result.instructions) {
      if (instr.mnemonic === 'DB') {
        ExecutionEngine.loadDbDirective(this.memory, instr);
      } else if (instr.mnemonic === 'DW') {
        ExecutionEngine.loadDwDirective(this.memory, instr);
      } else {
        executable.push(instr);
      }
    }

    this.instructions = executable;
    this.status = ExecutionStatus.Idle;

    // Start execution at the first executable instruction (skips .DATA section).
    if (executable.length > 0) {
      this.cpu.ip = executable[0].address;
    }

    this.instructionIndex.clear();
    for (let i = 0; i < this.instructions.length; i++) {
      this.instructionIndex.set(this.instructions[i].address, i);
    }

    // Compute the upper bound of the code/data area for stack overflow detection.
    this.codeDataEnd = 0;
    for (const instr of result.instructions) {
      const end = instr.address + instr.size;
      if (end > this.codeDataEnd) this.codeDataEnd = end;
    }

    return result;
  }

  private static loadDbDirective(memory: Memory, instr: ParsedInstruction): void {
    let addr = instr.address;
    for (const op of instr.operands) {
      if (typeof op.value === 'string') {
        // String literal — write each character byte
        for (let i = 0; i < op.value.length; i++) {
          memory.writeByte(addr, op.value.charCodeAt(i) & 0xFF);
          addr++;
        }
      } else {
        memory.writeByte(addr, (op.value as number) & 0xFF);
        addr++;
      }
    }
  }

  private static loadDwDirective(memory: Memory, instr: ParsedInstruction): void {
    let addr = instr.address;
    for (const op of instr.operands) {
      const val = typeof op.value === 'number' ? op.value : 0;
      memory.writeWord(addr, val & 0xFFFF);
      addr += 2;
    }
  }

  private static applyConfig(cpu: CpuState, config: ProgramConfig): void {
    if (!config.hasModel) {
      return;
    }

    // SP points to the top of the allocated stack region.
    // Stack grows downward from 0xFFFF, so SP = 0x10000 - stackSize
    // ensures the stack can grow `stackSize` bytes before overflowing.
    // Aligned to even address (word boundary) as 8086 requires.
    const top = 0x10000 - config.stackSize;
    cpu.sp = (top & 0xFFFE);
    cpu.ss = 0;
  }

  public step(): ExecutionResult {
    if (this.status === ExecutionStatus.Halted || this.status === ExecutionStatus.Error) {
      return this.buildResult(null);
    }

    const ip = this.cpu.ip;
    const idx = this.instructionIndex.get(ip);

    if (idx === undefined || idx >= this.instructions.length) {
      this.status = ExecutionStatus.Halted;
      return this.buildResult(null);
    }

    const instruction = this.instructions[idx];
    const ctx = this.createContext();
    const trace = ExecutionTraceFactory.create();
    ctx.trace = trace;
    const bufferLenBefore = this.consoleBuffer.length;

    try {
      const handler = this.registry.get(instruction.mnemonic);
      if (!handler) {
        throw new Error(`Unknown instruction: ${instruction.mnemonic}`);
      }

      // Advance IP before execution (branch instructions may override)
      this.cpu.ip = ip + instruction.size;

      handler(ctx, instruction.operands);

      // Sync back from context
      this.cpu = ctx.cpu;

      // Handle sleep request from INT 86h
      if (ctx.sleepMs !== undefined && ctx.sleepMs > 0) {
        this.sleepUntil = Date.now() + ctx.sleepMs;
      }

      // Stack overflow detection: SP must not enter the code/data area.
      if (this.codeDataEnd > 0 && this.cpu.sp < this.codeDataEnd) {
        throw new Error(
          `Stack overflow: SP (0x${this.cpu.sp.toString(16).toUpperCase().padStart(4, '0')}) collided with code/data area (ends at 0x${this.codeDataEnd.toString(16).toUpperCase().padStart(4, '0')})`,
        );
      }

      if (ctx.halted) {
        this.status = ExecutionStatus.Halted;
      } else {
        this.status = ExecutionStatus.Paused;
      }

      return this.buildResult(instruction, bufferLenBefore, trace);
    } catch (err) {
      this.status = ExecutionStatus.Error;
      this.error = err instanceof Error ? err.message : String(err);
      return this.buildResult(instruction, bufferLenBefore, trace);
    }
  }

  public run(onComplete?: () => void, onSleep?: () => void): void {
    if (this.status === ExecutionStatus.Halted || this.status === ExecutionStatus.Error) {
      onComplete?.();
      return;
    }

    this.status = ExecutionStatus.Running;
    let steps = 0;

    const tick = (): void => {
      if (this.status !== ExecutionStatus.Running) {
        onComplete?.();
        return;
      }

      // If a sleep is active, reschedule the tick after the remaining delay
      if (this.sleepUntil > 0) {
        const remaining = this.sleepUntil - Date.now();
        if (remaining > 0) {
          this.runHandle = setTimeout(tick, remaining);
          this.sleepUntil = 0;
          return;
        }
        this.sleepUntil = 0;
      }

      const batchSize = 1000;
      for (let i = 0; i < batchSize && this.status === ExecutionStatus.Running; i++) {
        this.step();
        // step() sets status to Paused; override back to Running if we should continue
        const currentStatus = this.status as ExecutionStatus;
        if (currentStatus === ExecutionStatus.Paused) {
          this.status = ExecutionStatus.Running;
        }

        steps++;

        // Check breakpoints (on the _next_ instruction)
        if (this.breakpoints.has(this.cpu.ip) && this.status === ExecutionStatus.Running) {
          this.status = ExecutionStatus.Paused;
          onComplete?.();
          return;
        }

        if (this.status !== ExecutionStatus.Running) {
          onComplete?.();
          return;
        }
        if (steps >= this.maxStepsPerRun) {
          this.status = ExecutionStatus.Error;
          this.error = `Exceeded maximum steps (${this.maxStepsPerRun})`;
          onComplete?.();
          return;
        }

        // If a sleep was triggered during this batch, yield immediately
        if (this.sleepUntil > 0) {
          const remaining = this.sleepUntil - Date.now();
          this.sleepUntil = 0;
          onSleep?.();
          this.runHandle = setTimeout(tick, Math.max(remaining, 0));
          return;
        }
      }

      if (this.status === ExecutionStatus.Running) {
        this.runHandle = setTimeout(tick, 0);
      } else {
        onComplete?.();
      }
    };

    tick();
  }

  public pause(): void {
    if (this.status === ExecutionStatus.Running) {
      this.status = ExecutionStatus.Paused;
      if (this.runHandle !== null) {
        clearTimeout(this.runHandle);
        this.runHandle = null;
      }
    }
  }

  public reset(): void {
    if (this.runHandle !== null) {
      clearTimeout(this.runHandle);
      this.runHandle = null;
    }
    this.cpu = CpuHelper.createInitialState();
    this.memory = new Memory();
    this.instructions = [];
    this.labels = new Map();
    this.status = ExecutionStatus.Idle;
    this.breakpoints.clear();
    this.error = null;
    this.consoleBuffer = [];
    this.consoleFlushMark = 0;
    this.instructionIndex.clear();
    this.codeDataEnd = 0;
    this.sleepUntil = 0;
    this.heap = new HeapAllocator(this.memory);
  }

  public setBreakpoint(address: number): void {
    this.breakpoints.add(address);
  }

  public removeBreakpoint(address: number): void {
    this.breakpoints.delete(address);
  }

  public setMaxSteps(max: number): void {
    this.maxStepsPerRun = max;
  }

  private createContext(): ExecutionContext {
    return {
      cpu: this.cpu,
      memory: this.memory,
      labels: this.labels,
      halted: false,
      onOutput: (text: string) => {
        this.consoleBuffer.push(text);
      },
      onInput: () => null,
      heap: this.heap,
    };
  }

  private buildResult(instruction: ParsedInstruction | null, bufferLenBefore?: number, trace?: ExecutionTrace): ExecutionResult {
    const from = bufferLenBefore ?? this.consoleBuffer.length;
    const newOutput = this.consoleBuffer.slice(from).join('');
    return {
      cpu: this.getCpu(),
      memorySnapshot: this.memory.snapshot(),
      status: this.status,
      instructionExecuted: instruction,
      output: newOutput.length > 0 ? newOutput : undefined,
      trace,
    };
  }
}
