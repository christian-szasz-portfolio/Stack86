import { describe, expect, it, vi } from 'vitest';
import { RetryPolicy } from './retry-policy';

describe('RetryPolicy', () => {
  it('returns immediately on first success', async () => {
    const op = vi.fn(async () => 'ok');
    const policy = new RetryPolicy({
      maxAttempts: 3,
      baseDelayMs: 1,
      maxDelayMs: 10,
      isTransient: () => true,
      delay: async () => undefined,
    });
    expect(await policy.executeAsync(op)).toBe('ok');
    expect(op).toHaveBeenCalledTimes(1);
  });

  it('retries transient errors up to maxAttempts', async () => {
    let calls = 0;
    const op = vi.fn(async () => {
      calls += 1;
      if (calls < 3) throw new Error('boom');
      return 'ok';
    });
    const policy = new RetryPolicy({
      maxAttempts: 3,
      baseDelayMs: 1,
      maxDelayMs: 10,
      isTransient: () => true,
      delay: async () => undefined,
    });
    expect(await policy.executeAsync(op)).toBe('ok');
    expect(op).toHaveBeenCalledTimes(3);
  });

  it('does not retry non-transient errors', async () => {
    const op = vi.fn(async () => {
      throw new Error('fatal');
    });
    const policy = new RetryPolicy({
      maxAttempts: 5,
      baseDelayMs: 1,
      maxDelayMs: 10,
      isTransient: () => false,
      delay: async () => undefined,
    });
    await expect(policy.executeAsync(op)).rejects.toThrow('fatal');
    expect(op).toHaveBeenCalledTimes(1);
  });

  it('throws the last error after exhausting attempts', async () => {
    const op = vi.fn(async () => {
      throw new Error('still bad');
    });
    const policy = new RetryPolicy({
      maxAttempts: 2,
      baseDelayMs: 1,
      maxDelayMs: 10,
      isTransient: () => true,
      delay: async () => undefined,
    });
    await expect(policy.executeAsync(op)).rejects.toThrow('still bad');
    expect(op).toHaveBeenCalledTimes(2);
  });

  it('uses exponential backoff with jitter when scheduling delays', async () => {
    const delays: number[] = [];
    let calls = 0;
    const op = vi.fn(async () => {
      calls += 1;
      if (calls < 4) throw new Error('boom');
      return 'ok';
    });
    const policy = new RetryPolicy({
      maxAttempts: 4,
      baseDelayMs: 100,
      maxDelayMs: 10_000,
      isTransient: () => true,
      delay: async (ms) => {
        delays.push(ms);
      },
      random: () => 0.999_999,
    });
    await policy.executeAsync(op);
    expect(delays).toEqual([99, 199, 399]);
  });
});
