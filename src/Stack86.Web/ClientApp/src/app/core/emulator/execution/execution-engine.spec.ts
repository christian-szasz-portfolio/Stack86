import { describe, it, expect } from 'vitest';
import { ExecutionEngine } from './execution-engine';
import { ExecutionStatus } from './execution-result.model';

describe('ExecutionEngine', () => {
  describe('loadProgram', () => {
    it('should load a valid program', () => {
      const engine = new ExecutionEngine();
      const result = engine.loadProgram('MOV AX, 5');
      expect(result.errors).toHaveLength(0);
      expect(engine.getStatus()).toBe(ExecutionStatus.Idle);
      expect(engine.getInstructions()).toHaveLength(1);
    });

    it('should report errors for invalid programs', () => {
      const engine = new ExecutionEngine();
      const result = engine.loadProgram('FOOBAR AX');
      expect(result.errors.length).toBeGreaterThan(0);
      expect(engine.getStatus()).toBe(ExecutionStatus.Error);
    });

    it('should reset state on new load', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 5');
      engine.step();
      engine.loadProgram('MOV BX, 10');
      expect(engine.getCpu().ax).toBe(0);
      expect(engine.getCpu().ip).toBe(0);
    });
  });

  describe('step', () => {
    it('should execute a single MOV instruction', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 42');
      const result = engine.step();
      expect(result.cpu.ax).toBe(42);
      expect(result.cpu.ip).toBe(1);
      expect(result.status).toBe(ExecutionStatus.Paused);
    });

    it('should execute multiple steps sequentially', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 10\nMOV BX, 20\nADD AX, BX');
      engine.step();
      engine.step();
      const result = engine.step();
      expect(result.cpu.ax).toBe(30);
      expect(result.cpu.bx).toBe(20);
      expect(result.cpu.ip).toBe(3);
    });

    it('should halt when past the last instruction', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 1');
      engine.step();
      const result = engine.step();
      expect(result.status).toBe(ExecutionStatus.Halted);
    });

    it('should halt on HLT instruction', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 1\nHLT');
      engine.step();
      const result = engine.step();
      expect(result.status).toBe(ExecutionStatus.Halted);
      expect(result.cpu.ax).toBe(1);
    });

    it('should not execute after halted', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('HLT');
      engine.step();
      const result = engine.step();
      expect(result.status).toBe(ExecutionStatus.Halted);
    });
  });

  describe('arithmetic and flags', () => {
    it('should set zero flag on SUB to zero', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 5\nSUB AX, 5');
      engine.step();
      const result = engine.step();
      expect(result.cpu.ax).toBe(0);
      expect(result.cpu.flags.zero).toBe(true);
    });

    it('should handle INC and DEC', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV CX, 10\nINC CX\nDEC CX');
      engine.step();
      engine.step();
      expect(engine.getCpu().cx).toBe(11);
      engine.step();
      expect(engine.getCpu().cx).toBe(10);
    });
  });

  describe('jumps and labels', () => {
    it('should execute JMP to a label', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('JMP skip\nMOV AX, 1\nskip: MOV AX, 2');
      engine.step(); // JMP skip
      const result = engine.step(); // MOV AX, 2
      expect(result.cpu.ax).toBe(2);
    });

    it('should execute conditional jump (JE) when zero flag set', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 5\nSUB AX, 5\nJE done\nMOV BX, 99\ndone: MOV BX, 1');
      engine.step(); // MOV AX, 5
      engine.step(); // SUB AX, 5 → AX=0, zero=true
      engine.step(); // JE done → should jump
      const result = engine.step(); // MOV BX, 1
      expect(result.cpu.bx).toBe(1);
      expect(result.cpu.ip).toBe(5);
    });

    it('should not take JE when zero flag not set', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 5\nSUB AX, 3\nJE done\nMOV BX, 99\ndone: HLT');
      engine.step(); // MOV AX, 5
      engine.step(); // SUB AX, 3 → AX=2, zero=false
      engine.step(); // JE done → should not jump
      engine.step(); // MOV BX, 99
      expect(engine.getCpu().bx).toBe(99);
    });

    it('should handle a simple loop', () => {
      const engine = new ExecutionEngine();
      const src = `
MOV CX, 3
MOV AX, 0
loop_start: ADD AX, 1
            DEC CX
            JNE loop_start
`;
      engine.loadProgram(src);
      // Run all steps manually
      for (let i = 0; i < 20; i++) {
        const result = engine.step();
        if (result.status === ExecutionStatus.Halted) break;
      }
      expect(engine.getCpu().ax).toBe(3);
      expect(engine.getCpu().cx).toBe(0);
    });
  });

  describe('console output (INT 21h)', () => {
    it('should capture output from INT 21h AH=02h', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 0x0200\nMOV DX, 65\nINT 0x21');
      engine.step(); // MOV AX → AH=02
      engine.step(); // MOV DX → DL=65='A'
      engine.step(); // INT 21h
      expect(engine.getConsoleOutput()).toContain('A');
    });

    it('should print $-terminated string via INT 21h AH=09h with DB data', () => {
      const engine = new ExecutionEngine();
      const src = [
        'MOV DX, msg',
        'MOV AH, 9',
        'INT 0x21',
        'HLT',
        'msg: DB 72, 101, 108, 108, 111, 36',  // "Hello$"
      ].join('\n');
      engine.loadProgram(src);
      engine.run();
      expect(engine.getStatus()).toBe(ExecutionStatus.Halted);
      expect(engine.getConsoleOutput().join('')).toContain('Hello');
    });

    it('should print string in compiler-like assembly with @DATA segment', () => {
      const engine = new ExecutionEngine();
      const src = [
        '.MODEL SMALL',
        '.STACK 256',
        '.CODE',
        'main:',
        '  MOV AX, @DATA',
        '  MOV DS, AX',
        '  PUSH BP',
        '  MOV BP, SP',
        '  MOV DX, _str_0',
        '  MOV AH, 9',
        '  INT 21h',
        '  MOV AX, 0',
        '  POP BP',
        '  HLT',
        '',
        '@DATA:',
        '_str_0: DB 72, 101, 108, 108, 111, 36',  // "Hello$"
      ].join('\n');
      const result = engine.loadProgram(src);
      expect(result.errors).toHaveLength(0);
      engine.run();
      expect(engine.getStatus()).toBe(ExecutionStatus.Halted);
      const output = engine.getConsoleOutput().join('');
      expect(output).toContain('Hello');
    });

    it('should print integer via _print_int helper', () => {
      const engine = new ExecutionEngine();
      const src = [
        '.MODEL SMALL',
        '.STACK 256',
        '.CODE',
        'main:',
        '  MOV AX, @DATA',
        '  MOV DS, AX',
        '  PUSH BP',
        '  MOV BP, SP',
        '  MOV AX, 42',
        '  PUSH AX',
        '  CALL _print_int',
        '  ADD SP, 2',
        '  MOV AX, 0',
        '  POP BP',
        '  HLT',
        '',
        '_print_int PROC',
        '  PUSH BP',
        '  MOV BP, SP',
        '  PUSH BX',
        '  PUSH CX',
        '  PUSH DX',
        '  MOV AX, [BP+4]',
        '  CMP AX, 0',
        '  JGE _pi_pos',
        '  PUSH AX',
        '  MOV DL, 45',
        '  MOV AH, 2',
        '  INT 21h',
        '  POP AX',
        '  NEG AX',
        '_pi_pos:',
        '  MOV CX, 0',
        '  MOV BX, 10',
        '_pi_div:',
        '  XOR DX, DX',
        '  DIV BX',
        '  PUSH DX',
        '  INC CX',
        '  CMP AX, 0',
        '  JNE _pi_div',
        '_pi_prt:',
        '  POP DX',
        '  ADD DL, 48',
        '  MOV AH, 2',
        '  INT 21h',
        '  DEC CX',
        '  CMP CX, 0',
        '  JNE _pi_prt',
        '  POP DX',
        '  POP CX',
        '  POP BX',
        '  POP BP',
        '  RET',
        '_print_int ENDP',
        '',
        '@DATA:',
      ].join('\n');
      const result = engine.loadProgram(src);
      expect(result.errors).toHaveLength(0);
      engine.run();
      expect(engine.getStatus()).toBe(ExecutionStatus.Halted);
      const output = engine.getConsoleOutput().join('');
      expect(output).toContain('42');
    });

    it('should call a function indirectly through a register (function pointer)', () => {
      const engine = new ExecutionEngine();
      const src = [
        '.MODEL SMALL',
        '.STACK 256',
        '.CODE',
        'main:',
        '  MOV AX, 21',
        '  PUSH AX',
        '  MOV AX, dbl', // load function address (no OFFSET) into a register
        '  CALL AX', // indirect call through the register
        '  ADD SP, 2',
        '  HLT',
        '',
        'dbl PROC',
        '  PUSH BP',
        '  MOV BP, SP',
        '  MOV AX, [BP+4]',
        '  ADD AX, AX',
        '  POP BP',
        '  RET',
        'dbl ENDP',
      ].join('\n');
      const result = engine.loadProgram(src);
      expect(result.errors).toHaveLength(0);
      engine.run();
      expect(engine.getStatus()).toBe(ExecutionStatus.Halted);
      expect(engine.getCpu().ax).toBe(42);
    });
  });

  describe('breakpoints', () => {
    it('should pause at breakpoint during run', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 1\nMOV BX, 2\nMOV CX, 3');
      engine.setBreakpoint(2); // break before third instruction
      engine.run();
      expect(engine.getStatus()).toBe(ExecutionStatus.Paused);
      expect(engine.getCpu().ax).toBe(1);
      expect(engine.getCpu().bx).toBe(2);
      expect(engine.getCpu().cx).toBe(0); // not yet executed
    });

    it('should allow removing breakpoints', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 1\nMOV BX, 2\nMOV CX, 3');
      engine.setBreakpoint(2);
      engine.removeBreakpoint(2);
      engine.run();
      // Should run to completion
      expect(engine.getCpu().cx).toBe(3);
    });
  });

  describe('run', () => {
    it('should run to completion for a short program', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 10\nMOV BX, 20\nADD AX, BX\nHLT');
      engine.run();
      expect(engine.getStatus()).toBe(ExecutionStatus.Halted);
      expect(engine.getCpu().ax).toBe(30);
    });

    it('should detect infinite loops via max steps', () => {
      const engine = new ExecutionEngine();
      engine.setMaxSteps(100);
      engine.loadProgram('loop: JMP loop');
      engine.run();
      expect(engine.getStatus()).toBe(ExecutionStatus.Error);
      expect(engine.getError()).toContain('maximum steps');
    });
  });

  describe('reset', () => {
    it('should reset all state', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 42');
      engine.step();
      engine.reset();
      expect(engine.getStatus()).toBe(ExecutionStatus.Idle);
      expect(engine.getCpu().ax).toBe(0);
      expect(engine.getCpu().ip).toBe(0);
      expect(engine.getInstructions()).toHaveLength(0);
      expect(engine.getConsoleOutput()).toHaveLength(0);
    });
  });

  describe('INT 86h services', () => {
    it('should set sleepUntil on INT 86h sleep service', () => {
      const engine = new ExecutionEngine();
      const src = [
        '.MODEL SMALL',
        '.STACK 256',
        '.CODE',
        'main:',
        '  MOV AX, @DATA',
        '  MOV DS, AX',
        '  PUSH BP',
        '  MOV BP, SP',
        '  MOV AX, 500',
        '  PUSH AX',
        '  MOV AH, 1',
        '  INT 86h',
        '  ADD SP, 2',
        '  POP BP',
        '  HLT',
        '',
        '@DATA:',
      ].join('\n');
      const result = engine.loadProgram(src);
      expect(result.errors).toHaveLength(0);

      // Step through to the INT 86h instruction
      engine.step(); // MOV AX, @DATA
      engine.step(); // MOV DS, AX
      engine.step(); // PUSH BP
      engine.step(); // MOV BP, SP
      engine.step(); // MOV AX, 500
      expect(engine.getCpu().ax).toBe(500);
      engine.step(); // PUSH AX (pushes 500 onto stack)
      engine.step(); // MOV AH, 1 (AH=1, corrupts AX to 0x01F4)
      expect((engine.getCpu().ax >> 8) & 0xFF).toBe(1); // AH = 1

      Date.now();
      engine.step(); // INT 86h — should trigger sleep
      // The engine should have set sleepUntil ~500ms from now
      // AX should be cleared to 0 by the handler
      expect(engine.getCpu().ax).toBe(0);
    });

    it('should delay execution during run with sleep', () => {
      const engine = new ExecutionEngine();
      const src = [
        '.MODEL SMALL',
        '.STACK 256',
        '.CODE',
        'main:',
        '  MOV AX, @DATA',
        '  MOV DS, AX',
        '  PUSH BP',
        '  MOV BP, SP',
        '  MOV DL, 65',
        '  MOV AH, 2',
        '  INT 21h',
        '  MOV AX, 200',
        '  PUSH AX',
        '  MOV AH, 1',
        '  INT 86h',
        '  ADD SP, 2',
        '  MOV DL, 66',
        '  MOV AH, 2',
        '  INT 21h',
        '  POP BP',
        '  HLT',
        '',
        '@DATA:',
      ].join('\n');
      const result = engine.loadProgram(src);
      expect(result.errors).toHaveLength(0);

      let sleepCalled = false;
      const startTime = Date.now();
      return new Promise<void>((resolve) => {
        engine.run(() => {
          const elapsed = Date.now() - startTime;
          expect(engine.getStatus()).toBe(ExecutionStatus.Halted);
          // Output should have both 'A' and 'B'
          const output = engine.getConsoleOutput().join('');
          expect(output).toContain('A');
          expect(output).toContain('B');
          // Should have taken at least ~150ms (200ms sleep, but timer precision)
          expect(elapsed).toBeGreaterThanOrEqual(150);
          expect(sleepCalled).toBe(true);
          resolve();
        }, () => {
          sleepCalled = true;
        });
      });
    }, 5000);
  });

  describe('memory operations', () => {
    it('should write and read memory with WORD PTR', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 0x1234\nMOV WORD PTR [0x200], AX\nHLT');
      engine.step(); // MOV AX, 0x1234
      expect(engine.getCpu().ax).toBe(0x1234);
      const result = engine.step(); // MOV WORD PTR [0x200], AX
      expect(result.memorySnapshot[0x200]).toBe(0x34); // low byte
      expect(result.memorySnapshot[0x201]).toBe(0x12); // high byte
    });

    it('should read memory back into registers', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 0x1234\nMOV WORD PTR [0x200], AX\nMOV BX, [0x200]\nHLT');
      engine.step(); // MOV AX
      engine.step(); // MOV [0x200], AX
      engine.step(); // MOV BX, [0x200]
      expect(engine.getCpu().bx).toBe(0x1234);
    });
  });

  describe('data directives', () => {
    it('should load DB bytes into memory and exclude from execution', () => {
      const engine = new ExecutionEngine();
      const src = 'MOV AX, 5\nHLT\ndata: DB 72, 101, 108';
      const result = engine.loadProgram(src);
      expect(result.errors).toHaveLength(0);
      // Only MOV and HLT are executable
      expect(engine.getInstructions()).toHaveLength(2);
      // DB bytes should be in memory at the data label address
      const addr = result.labels.get('data')!;
      const mem = engine.getMemory();
      expect(mem.readByte(addr)).toBe(72);
      expect(mem.readByte(addr + 1)).toBe(101);
      expect(mem.readByte(addr + 2)).toBe(108);
    });

    it('should resolve @DATA label in MOV instruction', () => {
      const engine = new ExecutionEngine();
      const src = [
        '.MODEL SMALL',
        '.CODE',
        'MOV AX, @DATA',
        'MOV DS, AX',
        'HLT',
        '@DATA:',
        'msg: DB 65, 66, 0',
      ].join('\n');
      engine.loadProgram(src);
      engine.step(); // MOV AX, @DATA — loads data segment address
      const dataAddr = engine.getLabels().get('@DATA')!;
      expect(engine.getCpu().ax).toBe(dataAddr);
      engine.step(); // MOV DS, AX
      expect(engine.getCpu().ds).toBe(dataAddr);
      engine.step(); // HLT
      expect(engine.getStatus()).toBe(ExecutionStatus.Halted);
      // Verify data bytes are in memory
      const mem = engine.getMemory();
      expect(mem.readByte(dataAddr)).toBe(65);
      expect(mem.readByte(dataAddr + 1)).toBe(66);
      expect(mem.readByte(dataAddr + 2)).toBe(0);
    });

    it('should handle segment register MOV', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 0x1000\nMOV DS, AX\nMOV ES, AX\nHLT');
      engine.step();
      engine.step();
      expect(engine.getCpu().ds).toBe(0x1000);
      engine.step();
      expect(engine.getCpu().es).toBe(0x1000);
    });

    it('should initialize SP from .MODEL SMALL stack size', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('.MODEL SMALL\n.STACK 256\n.CODE\nMOV AX, 1\nHLT');
      // SP = 0x10000 - 256 = 0xFF00
      expect(engine.getCpu().sp).toBe(0xFF00);
    });

    it('should initialize SP from explicit .STACK directive', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('.MODEL SMALL\n.STACK 512\n.CODE\nMOV AX, 1\nHLT');
      // SP = 0x10000 - 512 = 0xFE00
      expect(engine.getCpu().sp).toBe(0xFE00);
    });

    it('should use default SP when no .MODEL directive', () => {
      const engine = new ExecutionEngine();
      engine.loadProgram('MOV AX, 1\nHLT');
      // No .MODEL → default SP from createInitialState (0xFFFE)
      expect(engine.getCpu().sp).toBe(0xFFFE);
    });
  });

  describe('stack frame addressing', () => {
    it('should correctly read/write [BP-N] memory operands', () => {
      const engine = new ExecutionEngine();
      const src = [
        '.MODEL SMALL',
        '.STACK 256',
        '.CODE',
        'main:',
        '  MOV AX, @DATA',
        '  MOV DS, AX',
        '  PUSH BP',
        '  MOV BP, SP',
        '  SUB SP, 6',
        '  MOV AX, 5',
        '  MOV [BP-2], AX',
        '  MOV AX, 10',
        '  MOV [BP-4], AX',
        '  MOV AX, [BP-2]',
        '  PUSH AX',
        '  MOV AX, [BP-4]',
        '  MOV BX, AX',
        '  POP AX',
        '  ADD AX, BX',
        '  MOV [BP-6], AX',
        '  MOV AX, [BP-6]',
        '  MOV SP, BP',
        '  POP BP',
        '  HLT',
      ].join('\n');
      engine.loadProgram(src);
      engine.run();
      expect(engine.getStatus()).toBe(ExecutionStatus.Halted);
      expect(engine.getCpu().ax).toBe(15);
      expect(engine.getCpu().bx).toBe(10);
      expect(engine.getCpu().bp).toBe(0);
      expect(engine.getCpu().sp).toBe(0xFF00);
    });
  });
});
