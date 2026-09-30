import { describe, it, expect } from 'vitest';
import { ExecutionStatus } from '@core/emulator/execution/execution-result.model';
import { SAMPLE_PROGRAMS } from './sample-programs';

/**
 * Tests the toolbar computed state logic used by ToolbarComponent.
 * Mirrors the computed signal transformations without Angular DI.
 */

function isRunning(status: ExecutionStatus): boolean {
  return status === ExecutionStatus.Running;
}

function canRun(status: ExecutionStatus): boolean {
  return status === ExecutionStatus.Idle || status === ExecutionStatus.Paused;
}

function canStep(status: ExecutionStatus): boolean {
  return canRun(status);
}

function canPause(status: ExecutionStatus): boolean {
  return isRunning(status);
}

describe('ToolbarComponent logic', () => {
  describe('status-derived states', () => {
    it('should allow run when idle', () => {
      expect(canRun(ExecutionStatus.Idle)).toBe(true);
    });

    it('should allow run when paused', () => {
      expect(canRun(ExecutionStatus.Paused)).toBe(true);
    });

    it('should not allow run when running', () => {
      expect(canRun(ExecutionStatus.Running)).toBe(false);
    });

    it('should not allow run when halted', () => {
      expect(canRun(ExecutionStatus.Halted)).toBe(false);
    });

    it('should not allow run when error', () => {
      expect(canRun(ExecutionStatus.Error)).toBe(false);
    });

    it('should allow step when idle', () => {
      expect(canStep(ExecutionStatus.Idle)).toBe(true);
    });

    it('should allow step when paused', () => {
      expect(canStep(ExecutionStatus.Paused)).toBe(true);
    });

    it('should not allow step when running', () => {
      expect(canStep(ExecutionStatus.Running)).toBe(false);
    });

    it('should allow pause only when running', () => {
      expect(canPause(ExecutionStatus.Running)).toBe(true);
      expect(canPause(ExecutionStatus.Idle)).toBe(false);
      expect(canPause(ExecutionStatus.Paused)).toBe(false);
      expect(canPause(ExecutionStatus.Halted)).toBe(false);
    });

    it('should report isRunning correctly', () => {
      expect(isRunning(ExecutionStatus.Running)).toBe(true);
      expect(isRunning(ExecutionStatus.Idle)).toBe(false);
    });
  });

  describe('sample programs', () => {
    it('should have at least one sample', () => {
      expect(SAMPLE_PROGRAMS.length).toBeGreaterThan(0);
    });

    it('should have name and source for each sample', () => {
      SAMPLE_PROGRAMS.forEach(sample => {
        expect(sample.name).toBeTruthy();
        expect(sample.source).toBeTruthy();
      });
    });

    it('should have non-empty source code', () => {
      SAMPLE_PROGRAMS.forEach(sample => {
        expect(sample.source.trim().length).toBeGreaterThan(0);
      });
    });

    it('should have unique names', () => {
      const names = SAMPLE_PROGRAMS.map(s => s.name);
      expect(new Set(names).size).toBe(names.length);
    });
  });

  describe('localStorage persistence', () => {
    it('should save and retrieve source code', () => {
      const key = 'stack86_source';
      const source = 'MOV AX, 1\nHLT';
      localStorage.setItem(key, source);
      expect(localStorage.getItem(key)).toBe(source);
      localStorage.removeItem(key);
    });
  });
});
