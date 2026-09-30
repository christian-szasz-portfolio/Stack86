import { describe, expect, it } from 'vitest';
import { ExecutionStatus } from './execution-result.model';

describe('ExecutionStatus enum', () => {
  it('exposes idle, running, paused, halted, error', () => {
    expect(ExecutionStatus.Idle).toBe('idle');
    expect(ExecutionStatus.Running).toBe('running');
    expect(ExecutionStatus.Paused).toBe('paused');
    expect(ExecutionStatus.Halted).toBe('halted');
    expect(ExecutionStatus.Error).toBe('error');
  });
});
