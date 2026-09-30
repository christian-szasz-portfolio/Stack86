import { CpuState } from '../cpu/cpu.model';
import { ParsedInstruction } from '../instruction/instruction.model';
import { ExecutionTrace } from './execution-trace.model';

export enum ExecutionStatus {
  Idle = 'idle',
  Running = 'running',
  Paused = 'paused',
  Halted = 'halted',
  Error = 'error',
}

export interface ExecutionResult {
  cpu: CpuState;
  memorySnapshot: Uint8Array;
  status: ExecutionStatus;
  instructionExecuted: ParsedInstruction | null;
  output?: string;
  trace?: ExecutionTrace;
}
