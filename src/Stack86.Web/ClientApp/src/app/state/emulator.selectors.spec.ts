import { describe, it, expect } from 'vitest';
import {
  selectCpu,
  selectRegisters,
  selectFlags,
  selectMemory,
  selectInstructions,
  selectCurrentInstruction,
  selectExecutionStatus,
  selectBreakpoints,
  selectConsoleOutput,
  selectError,
  selectSourceCode,
  selectStackView,
  selectMemorySlice,
} from './emulator.selectors';
import { EmulatorState } from './emulator.state';
import { ExecutionStatus } from '../core/emulator/execution/execution-result.model';
import { CpuHelper } from '../core/emulator/cpu/cpu.model';

describe('emulator selectors', () => {
  const state: EmulatorState = {
    cpu: { ...CpuHelper.createInitialState(), ax: 0x1234, bx: 0x5678, ip: 2 },
    memory: Array.from({ length: 0x10000 }, () => 0),
    instructions: [
      { mnemonic: 'MOV', operands: [], line: 0, address: 0, size: 1, source: 'MOV AX, 1' },
      { mnemonic: 'ADD', operands: [], line: 1, address: 1, size: 1, source: 'ADD AX, 2' },
      { mnemonic: 'HLT', operands: [], line: 2, address: 2, size: 1, source: 'HLT' },
    ],
    labels: { start: 0 },
    status: ExecutionStatus.Paused,
    breakpoints: [1, 3],
    consoleOutput: ['A', 'B'],
    error: null,
    sourceCode: 'MOV AX, 1\nADD AX, 2\nHLT',
    parseErrors: [],
  };

  const rootState = { emulator: state };

  it('selectCpu should return cpu state', () => {
    expect(selectCpu(rootState)).toBe(state.cpu);
  });

  it('selectRegisters should return register values', () => {
    const regs = selectRegisters(rootState);
    expect(regs.ax).toBe(0x1234);
    expect(regs.bx).toBe(0x5678);
    expect(regs.ip).toBe(2);
  });

  it('selectFlags should return flags', () => {
    const flags = selectFlags(rootState);
    expect(flags.zero).toBe(false);
  });

  it('selectMemory should return memory array', () => {
    expect(selectMemory(rootState)).toBe(state.memory);
  });

  it('selectInstructions should return instructions', () => {
    expect(selectInstructions(rootState)).toHaveLength(3);
  });

  it('selectCurrentInstruction should find instruction at IP', () => {
    const inst = selectCurrentInstruction(rootState);
    expect(inst?.mnemonic).toBe('HLT');
    expect(inst?.address).toBe(2);
  });

  it('selectCurrentInstruction should return null when no match', () => {
    const s = { emulator: { ...state, cpu: { ...state.cpu, ip: 99 } } };
    expect(selectCurrentInstruction(s)).toBeNull();
  });

  it('selectExecutionStatus should return status', () => {
    expect(selectExecutionStatus(rootState)).toBe(ExecutionStatus.Paused);
  });

  it('selectBreakpoints should return breakpoints', () => {
    expect(selectBreakpoints(rootState)).toEqual([1, 3]);
  });

  it('selectConsoleOutput should return output', () => {
    expect(selectConsoleOutput(rootState)).toEqual(['A', 'B']);
  });

  it('selectError should return null when no error', () => {
    expect(selectError(rootState)).toBeNull();
  });

  it('selectSourceCode should return source', () => {
    expect(selectSourceCode(rootState)).toBe('MOV AX, 1\nADD AX, 2\nHLT');
  });

  it('selectMemorySlice should return a slice', () => {
    const slice = selectMemorySlice(0, 4)(rootState);
    expect(slice).toHaveLength(4);
  });

  it('selectStackView should return stack entries', () => {
    const s = {
      emulator: {
        ...state,
        cpu: { ...state.cpu, sp: 0xFFFA },
        memory: Array.from({ length: 0x10000 }, () => 0),
      },
    };
    // Write some values at sp
    s.emulator.memory[0xFFFA] = 0x34;
    s.emulator.memory[0xFFFB] = 0x12;
    const stack = selectStackView(s);
    expect(stack.length).toBeGreaterThan(0);
    expect(stack[0].address).toBe(0xFFFA);
    expect(stack[0].value).toBe(0x1234);
  });
});
