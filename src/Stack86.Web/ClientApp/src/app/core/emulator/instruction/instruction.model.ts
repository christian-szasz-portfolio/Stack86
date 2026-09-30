import { CpuState } from '../cpu/cpu.model';
import { Memory } from '../memory/memory.model';
import { HeapAllocator } from '../memory/heap-allocator';
import { ExecutionTrace } from '../execution/execution-trace.model';

export enum OperandType {
  Register = 'register',
  Immediate = 'immediate',
  Memory = 'memory',
  Label = 'label',
}

export enum OperandSize {
  Byte = 'byte',
  Word = 'word',
}

export interface Operand {
  type: OperandType;
  value: string | number;
  size?: OperandSize;
  /** Displacement for memory expressions like [BP-2] → offset = -2 */
  offset?: number;
}

export interface ParsedInstruction {
  mnemonic: string;
  operands: Operand[];
  /** Original source line number (0-based) */
  line: number;
  /** Address in memory where this instruction is loaded */
  address: number;
  /** Size in bytes this instruction occupies */
  size: number;
  /** Original source text */
  source: string;
}

export interface ExecutionContext {
  cpu: CpuState;
  memory: Memory;
  /** Map of label name → address, resolved during assembly */
  labels: Map<string, number>;
  /** Callback for INT 21h / output */
  onOutput?: (text: string) => void;
  /** Callback for INT 21h / input request */
  onInput?: () => string | null;
  /** Flag set by HLT instruction */
  halted: boolean;
  /** Set by INT 86h sleep service — milliseconds to pause execution */
  sleepMs?: number;
  /** Heap allocator for malloc/free/calloc/realloc (INT 86h services) */
  heap: HeapAllocator;
  /** Optional trace recorder for data flow visualization */
  trace?: ExecutionTrace;
}

export type InstructionHandler = (
  ctx: ExecutionContext,
  operands: Operand[],
) => void;
