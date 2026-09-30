import { describe, expect, it } from 'vitest';
import { ExecutionEngine } from './execution-engine';
import { ExecutionStatus } from './execution-result.model';

describe('ExecutionEngine — extended behaviors', () => {
  describe('breakpoints', () => {
    it('exposes add/remove via the breakpoint set', () => {
      const engine = new ExecutionEngine();
      engine.setBreakpoint(2);
      expect(engine.getBreakpoints().has(2)).toBe(true);
      engine.removeBreakpoint(2);
      expect(engine.getBreakpoints().has(2)).toBe(false);
    });

    it('pauses run() when reaching a breakpointed address', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 1\nMOV BX, 2\nMOV CX, 3\nHLT');
      engine.setBreakpoint(2); // address of 3rd instruction (MOV CX, 3)
      engine.run();
      expect(engine.getStatus()).toBe(ExecutionStatus.Paused);
      expect(engine.getCpu().ax).toBe(1);
      expect(engine.getCpu().bx).toBe(2);
      expect(engine.getCpu().cx).toBe(0);
      expect(engine.getCpu().ip).toBe(2);
    });

    it('clears breakpoints on reset()', () => {
      const engine = new ExecutionEngine();
      engine.setBreakpoint(5);
      engine.reset();
      expect(engine.getBreakpoints().size).toBe(0);
    });
  });

  describe('setMaxSteps', () => {
    it('marks the engine in Error status when exceeded', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('start: JMP start');
      engine.setMaxSteps(50);
      engine.run();
      expect(engine.getStatus()).toBe(ExecutionStatus.Error);
      expect(engine.getError()).toMatch(/maximum steps/i);
    });
  });

  describe('reset', () => {
    it('clears CPU state, instructions and console buffer', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 5\nHLT');
      engine.step();
      engine.reset();

      expect(engine.getCpu().ax).toBe(0);
      expect(engine.getCpu().ip).toBe(0);
      expect(engine.getInstructions().length).toBe(0);
      expect(engine.getStatus()).toBe(ExecutionStatus.Idle);
      expect(engine.getError()).toBeNull();
      expect(engine.getConsoleOutput()).toEqual([]);
    });
  });

  describe('flushConsoleOutput', () => {
    it('returns only new output since the previous flush', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram(
        [
          'MOV AX, 0x0200', // AH=02 (char output)
          'MOV DX, 65',     // 'A'
          'INT 0x21',
          'MOV DX, 66',     // 'B'
          'INT 0x21',
          'HLT',
        ].join('\n'),
      );
      engine.step(); // MOV AX, 0x0200
      engine.step(); // MOV DX, 65
      engine.step(); // INT 21h → emits 'A'
      const first = engine.flushConsoleOutput();
      expect(first.join('')).toContain('A');

      engine.step(); // MOV DX, 66
      engine.step(); // INT 21h → emits 'B'
      const second = engine.flushConsoleOutput();
      expect(second.join('')).toContain('B');
      expect(second.join('')).not.toContain('A');
    });
  });

  describe('error states', () => {
    it('halts with error status on parse failure', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('THIS_IS_NOT_AN_INSTRUCTION');
      expect(engine.getStatus()).toBe(ExecutionStatus.Error);
      expect(engine.getError()).not.toBeNull();
    });

    it('step is a no-op once in Error state', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('FOOBAR');
      const before = engine.getCpu().ip;
      engine.step();
      expect(engine.getCpu().ip).toBe(before);
      expect(engine.getStatus()).toBe(ExecutionStatus.Error);
    });
  });

  describe('getCpu', () => {
    it('returns a defensive copy (caller mutations do not leak)', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 5');
      engine.step();
      const snapshot = engine.getCpu();
      snapshot.ax = 0xDEAD;
      snapshot.flags.zero = !snapshot.flags.zero;
      expect(engine.getCpu().ax).toBe(5);
    });
  });
});
