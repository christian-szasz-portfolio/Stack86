import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { CpuState } from '../core/emulator/cpu/cpu.model';
import { ParsedInstruction } from '../core/emulator/instruction/instruction.model';
import { ExecutionTrace } from '../core/emulator/execution/execution-trace.model';
import { SerializedParseError } from './emulator.state';

export const EmulatorActions = createActionGroup({
  source: 'Emulator',
  events: {
    'Load Program': props<{ source: string }>(),
    'Load Program Success': props<{
      instructions: ParsedInstruction[];
      labels: Record<string, number>;
      memory: number[];
      logMessages: string[];
    }>(),
    'Load Program Failure': props<{ error: string; parseErrors: SerializedParseError[] }>(),
    'Step': emptyProps(),
    'Step Success': props<{
      cpu: CpuState;
      memory: number[];
      status: string;
      output?: string;
      consoleOutput?: string[];
      trace?: ExecutionTrace;
    }>(),
    'Run': emptyProps(),
    'Pause': emptyProps(),
    'Reset': emptyProps(),
    'Execution Error': props<{ error: string }>(),
    'Set Breakpoint': props<{ address: number }>(),
    'Remove Breakpoint': props<{ address: number }>(),
    'Console Output': props<{ text: string }>(),
    'Update Source Code': props<{ source: string }>(),
    'Update Status': props<{ status: string }>(),
    'Export Com': emptyProps(),
    'Import Com': emptyProps(),
    'Export Asm': emptyProps(),
    'Import Asm': emptyProps(),
  },
});

export type EmulatorEffectAction =
  | ReturnType<typeof EmulatorActions.stepSuccess>
  | ReturnType<typeof EmulatorActions.executionError>;
