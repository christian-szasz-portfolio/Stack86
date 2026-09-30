import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { IdleTimeoutController } from './idle-timeout-controller';

describe('IdleTimeoutController', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('aborts the signal once idleMs elapses', () => {
    const ctl = new IdleTimeoutController({ idleMs: 1000 });
    ctl.start();
    expect(ctl.signal.aborted).toBe(false);
    vi.advanceTimersByTime(999);
    expect(ctl.signal.aborted).toBe(false);
    vi.advanceTimersByTime(2);
    expect(ctl.signal.aborted).toBe(true);
  });

  it('bumpActivity resets the timer', () => {
    const ctl = new IdleTimeoutController({ idleMs: 1000 });
    ctl.start();
    vi.advanceTimersByTime(800);
    ctl.bumpActivity();
    vi.advanceTimersByTime(800);
    expect(ctl.signal.aborted).toBe(false);
    vi.advanceTimersByTime(300);
    expect(ctl.signal.aborted).toBe(true);
  });

  it('dispose stops further aborts', () => {
    const ctl = new IdleTimeoutController({ idleMs: 100 });
    ctl.start();
    ctl.dispose();
    vi.advanceTimersByTime(500);
    expect(ctl.signal.aborted).toBe(false);
  });

  it('explicit abort propagates a custom reason', () => {
    const ctl = new IdleTimeoutController({ idleMs: 100 });
    ctl.start();
    ctl.abort('cancelled');
    expect(ctl.signal.aborted).toBe(true);
    expect(ctl.signal.reason).toBe('cancelled');
  });
});
