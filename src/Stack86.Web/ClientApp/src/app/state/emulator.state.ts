import { CpuState, CpuHelper } from '../core/emulator/cpu/cpu.model';
import { ParsedInstruction } from '../core/emulator/instruction/instruction.model';
import { ExecutionStatus } from '../core/emulator/execution/execution-result.model';
import { ExecutionTrace } from '../core/emulator/execution/execution-trace.model';

export const EMULATOR_FEATURE_KEY = 'emulator';

export interface SerializedParseError {
  line: number;
  column: number;
  message: string;
  severity: string;
}

export interface EmulatorState {
  cpu: CpuState;
  memory: number[];
  instructions: ParsedInstruction[];
  labels: Record<string, number>;
  status: ExecutionStatus;
  breakpoints: number[];
  consoleOutput: string[];
  error: string | null;
  sourceCode: string;
  parseErrors: SerializedParseError[];
  lastTrace: ExecutionTrace | null;
}

export const initialEmulatorState: EmulatorState = {
  cpu: CpuHelper.createInitialState(),
  memory: Array.from(new Uint8Array(0x10000)),
  instructions: [],
  labels: {},
  status: ExecutionStatus.Idle,
  breakpoints: [],
  consoleOutput: [],
  error: null,
  sourceCode: '',
  parseErrors: [],
  lastTrace: null,
};
