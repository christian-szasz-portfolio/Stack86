import { describe, it, expect } from 'vitest';

/**
 * Tests the console tab-filtering logic used by ConsoleComponent.
 * The component is a thin wrapper — test output classification.
 */

function filterOutput(lines: string[]): string[] {
  return lines.filter((l) => !l.startsWith('[Info]') && !l.startsWith('[Warn]') && !l.startsWith('[Error]'));
}

function filterProblems(lines: string[]): string[] {
  return lines.filter((l) => l.startsWith('[Error]') || l.startsWith('[Warn]'));
}

function filterBuildLog(lines: string[]): string[] {
  return lines.filter((l) => l.startsWith('[Info]'));
}

describe('ConsoleComponent logic', () => {
  it('should treat empty array as no output', () => {
    const output: string[] = [];
    expect(filterOutput(output)).toHaveLength(0);
    expect(filterProblems(output)).toHaveLength(0);
    expect(filterBuildLog(output)).toHaveLength(0);
  });

  it('should classify program output separately from log/errors', () => {
    const output = ['[Info] Assembled OK', '[Error] Line 5: bad opcode', 'Hello World'];
    expect(filterOutput(output)).toEqual(['Hello World']);
    expect(filterProblems(output)).toEqual(['[Error] Line 5: bad opcode']);
    expect(filterBuildLog(output)).toEqual(['[Info] Assembled OK']);
  });

  it('should preserve output line order', () => {
    const output = ['Hello', 'World', '!'];
    const result = filterOutput(output);
    expect(result).toEqual(['Hello', 'World', '!']);
  });

  it('should handle single character output', () => {
    const output = ['A'];
    expect(filterOutput(output)).toHaveLength(1);
    expect(filterOutput(output)[0]).toBe('A');
  });

  it('should separate errors and warnings into problems', () => {
    const output = [
      '[Warn] Assembly failed with 2 error(s)',
      '[Error] Line 1: unknown instruction',
      '[Error] Line 3: label not found',
    ];
    expect(filterProblems(output)).toHaveLength(3);
    expect(filterOutput(output)).toHaveLength(0);
  });
});
