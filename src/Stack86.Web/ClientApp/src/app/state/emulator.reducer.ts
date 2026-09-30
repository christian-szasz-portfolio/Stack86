import { createReducer, on } from '@ngrx/store';
import { EmulatorActions } from './emulator.actions';
import { EmulatorState, initialEmulatorState } from './emulator.state';
import { ExecutionStatus } from '../core/emulator/execution/execution-result.model';
import { CpuHelper } from '../core/emulator/cpu/cpu.model';

export const emulatorReducer = createReducer(
  initialEmulatorState,

  on(EmulatorActions.updateSourceCode, (state, { source }): EmulatorState => ({
    ...state,
    sourceCode: source,
  })),

  on(EmulatorActions.loadProgram, (state, { source }): EmulatorState => ({
    ...state,
    sourceCode: source,
    error: null,
    parseErrors: [],
    status: ExecutionStatus.Idle,
  })),

  on(EmulatorActions.loadProgramSuccess, (state, { instructions, labels, memory, logMessages }): EmulatorState => ({
    ...state,
    instructions,
    labels,
    cpu: CpuHelper.createInitialState(),
    memory,
    consoleOutput: logMessages,
    status: ExecutionStatus.Idle,
    error: null,
    parseErrors: [],
    lastTrace: null,
  })),

  on(EmulatorActions.loadProgramFailure, (state, { error, parseErrors }): EmulatorState => ({
    ...state,
    status: ExecutionStatus.Error,
    error,
    parseErrors,
    consoleOutput: [
      ...state.consoleOutput,
      `[Warn] Assembly failed with ${parseErrors.length} error(s)`,
      ...parseErrors.map((e) => `[Error] Line ${e.line + 1}: ${e.message}`),
    ],
  })),

  on(EmulatorActions.run, (state): EmulatorState => ({
    ...state,
    status: ExecutionStatus.Running,
  })),

  on(EmulatorActions.pause, (state): EmulatorState => ({
    ...state,
    status: ExecutionStatus.Paused,
  })),

  on(EmulatorActions.stepSuccess, (state, { cpu, memory, status, output, consoleOutput, trace }): EmulatorState => ({
    ...state,
    cpu,
    memory,
    status: status as ExecutionStatus,
    consoleOutput: consoleOutput ?? (output ? [...state.consoleOutput, output] : state.consoleOutput),
    lastTrace: trace ?? null,
  })),

  on(EmulatorActions.executionError, (state, { error }): EmulatorState => ({
    ...state,
    status: ExecutionStatus.Error,
    error,
    consoleOutput: [...state.consoleOutput, `[Error] Runtime: ${error}`],
  })),

  on(EmulatorActions.reset, (state): EmulatorState => ({
    ...initialEmulatorState,
    sourceCode: state.sourceCode,
    parseErrors: [],
    lastTrace: null,
  })),

  on(EmulatorActions.setBreakpoint, (state, { address }): EmulatorState => ({
    ...state,
    breakpoints: state.breakpoints.includes(address) ? state.breakpoints : [...state.breakpoints, address],
  })),

  on(EmulatorActions.removeBreakpoint, (state, { address }): EmulatorState => ({
    ...state,
    breakpoints: state.breakpoints.filter((a) => a !== address),
  })),

  on(EmulatorActions.consoleOutput, (state, { text }): EmulatorState => ({
    ...state,
    consoleOutput: [...state.consoleOutput, text],
  })),

  on(EmulatorActions.updateStatus, (state, { status }): EmulatorState => ({
    ...state,
    status: status as ExecutionStatus,
  })),
);
