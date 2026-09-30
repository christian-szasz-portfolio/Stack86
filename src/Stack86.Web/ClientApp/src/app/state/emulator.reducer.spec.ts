import { describe, it, expect } from 'vitest';
import { emulatorReducer } from './emulator.reducer';
import { EmulatorActions } from './emulator.actions';
import { initialEmulatorState, EmulatorState } from './emulator.state';
import { ExecutionStatus } from '../core/emulator/execution/execution-result.model';
import { CpuHelper } from '../core/emulator/cpu/cpu.model';

describe('emulatorReducer', () => {
  it('should return initial state', () => {
    const state = emulatorReducer(undefined, { type: 'unknown' });
    expect(state).toEqual(initialEmulatorState);
  });

  it('should handle updateSourceCode', () => {
    const state = emulatorReducer(
      initialEmulatorState,
      EmulatorActions.updateSourceCode({ source: 'MOV AX, 1' }),
    );
    expect(state.sourceCode).toBe('MOV AX, 1');
  });

  it('should handle loadProgramSuccess', () => {
    const instructions = [
      { mnemonic: 'MOV', operands: [], line: 0, address: 0, size: 1, source: 'MOV AX, 1' },
    ];
    const labels = { start: 0 };
    const logMessages = ['[Info] Assembling 1 lines...', '[Info] Build successful'];
    const state = emulatorReducer(
      initialEmulatorState,
      EmulatorActions.loadProgramSuccess({ instructions, labels, memory: [0], logMessages }),
    );
    expect(state.instructions).toEqual(instructions);
    expect(state.labels).toEqual(labels);
    expect(state.status).toBe(ExecutionStatus.Idle);
    expect(state.error).toBeNull();
    expect(state.consoleOutput).toEqual(logMessages);
  });

  it('should handle loadProgramFailure', () => {
    const state = emulatorReducer(
      initialEmulatorState,
      EmulatorActions.loadProgramFailure({ error: 'Parse error', parseErrors: [{ line: 0, column: 0, message: 'Parse error', severity: 'error' }] }),
    );
    expect(state.status).toBe(ExecutionStatus.Error);
    expect(state.error).toBe('Parse error');
    expect(state.parseErrors).toHaveLength(1);
    expect(state.consoleOutput).toContainEqual(expect.stringContaining('[Error]'));
  });

  it('should handle stepSuccess', () => {
    const cpu = { ...CpuHelper.createInitialState(), ax: 42, ip: 1 };
    const memory = [0, 0, 0];
    const state = emulatorReducer(
      initialEmulatorState,
      EmulatorActions.stepSuccess({ cpu, memory, status: ExecutionStatus.Paused }),
    );
    expect(state.cpu.ax).toBe(42);
    expect(state.cpu.ip).toBe(1);
    expect(state.memory).toEqual([0, 0, 0]);
    expect(state.status).toBe(ExecutionStatus.Paused);
  });

  it('should append console output on stepSuccess with output', () => {
    const cpu = CpuHelper.createInitialState();
    const state = emulatorReducer(
      initialEmulatorState,
      EmulatorActions.stepSuccess({ cpu, memory: [], status: ExecutionStatus.Paused, output: 'A' }),
    );
    expect(state.consoleOutput).toEqual(['A']);
  });

  it('should handle executionError', () => {
    const state = emulatorReducer(
      initialEmulatorState,
      EmulatorActions.executionError({ error: 'Division by zero' }),
    );
    expect(state.status).toBe(ExecutionStatus.Error);
    expect(state.error).toBe('Division by zero');
    expect(state.consoleOutput).toContainEqual(expect.stringContaining('[Error] Runtime:'));
  });

  it('should handle reset', () => {
    const modified: EmulatorState = {
      ...initialEmulatorState,
      cpu: { ...CpuHelper.createInitialState(), ax: 99 },
      status: ExecutionStatus.Paused,
      error: 'old error',
      consoleOutput: ['foo'],
    };
    const state = emulatorReducer(modified, EmulatorActions.reset());
    expect(state).toEqual(initialEmulatorState);
  });

  it('should handle setBreakpoint', () => {
    const state = emulatorReducer(
      initialEmulatorState,
      EmulatorActions.setBreakpoint({ address: 5 }),
    );
    expect(state.breakpoints).toContain(5);
  });

  it('should not duplicate breakpoints', () => {
    let state = emulatorReducer(initialEmulatorState, EmulatorActions.setBreakpoint({ address: 5 }));
    state = emulatorReducer(state, EmulatorActions.setBreakpoint({ address: 5 }));
    expect(state.breakpoints.filter((a) => a === 5)).toHaveLength(1);
  });

  it('should handle removeBreakpoint', () => {
    let state = emulatorReducer(initialEmulatorState, EmulatorActions.setBreakpoint({ address: 5 }));
    state = emulatorReducer(state, EmulatorActions.removeBreakpoint({ address: 5 }));
    expect(state.breakpoints).not.toContain(5);
  });

  it('should handle consoleOutput', () => {
    const state = emulatorReducer(
      initialEmulatorState,
      EmulatorActions.consoleOutput({ text: 'Hello' }),
    );
    expect(state.consoleOutput).toEqual(['Hello']);
  });
});
