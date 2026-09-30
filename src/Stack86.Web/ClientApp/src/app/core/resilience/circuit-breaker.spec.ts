import { describe, expect, it } from 'vitest';
import { CircuitBreaker, CircuitBreakerState } from './circuit-breaker';

describe('CircuitBreaker', () => {
  it('starts closed and allows execution', () => {
    const cb = new CircuitBreaker({ failureThreshold: 2, cooldownMs: 100 });
    expect(cb.getState()).toBe(CircuitBreakerState.Closed);
    expect(cb.canExecute()).toBe(true);
  });

  it('trips open after threshold consecutive failures', () => {
    const cb = new CircuitBreaker({ failureThreshold: 3, cooldownMs: 1000, now: () => 0 });
    cb.recordFailure();
    cb.recordFailure();
    expect(cb.getState()).toBe(CircuitBreakerState.Closed);
    cb.recordFailure();
    expect(cb.getState()).toBe(CircuitBreakerState.Open);
    expect(cb.canExecute()).toBe(false);
  });

  it('a single success resets the failure count', () => {
    const cb = new CircuitBreaker({ failureThreshold: 3, cooldownMs: 1000, now: () => 0 });
    cb.recordFailure();
    cb.recordFailure();
    cb.recordSuccess();
    cb.recordFailure();
    expect(cb.getState()).toBe(CircuitBreakerState.Closed);
  });

  it('transitions Open -> HalfOpen after cooldown elapses', () => {
    let now = 0;
    const cb = new CircuitBreaker({ failureThreshold: 1, cooldownMs: 500, now: () => now });
    cb.recordFailure();
    expect(cb.getState()).toBe(CircuitBreakerState.Open);
    now = 600;
    expect(cb.getState()).toBe(CircuitBreakerState.HalfOpen);
    expect(cb.canExecute()).toBe(true);
  });

  it('HalfOpen failure reopens the breaker', () => {
    let now = 0;
    const cb = new CircuitBreaker({ failureThreshold: 1, cooldownMs: 500, now: () => now });
    cb.recordFailure();
    now = 600;
    cb.getState();
    cb.recordFailure();
    expect(cb.getState()).toBe(CircuitBreakerState.Open);
  });

  it('HalfOpen success closes the breaker', () => {
    let now = 0;
    const cb = new CircuitBreaker({ failureThreshold: 1, cooldownMs: 500, now: () => now });
    cb.recordFailure();
    now = 600;
    cb.getState();
    cb.recordSuccess();
    expect(cb.getState()).toBe(CircuitBreakerState.Closed);
  });

  it('reports remaining cooldown only while Open', () => {
    let now = 0;
    const cb = new CircuitBreaker({ failureThreshold: 1, cooldownMs: 1000, now: () => now });
    expect(cb.remainingCooldownMs()).toBe(0);
    cb.recordFailure();
    now = 200;
    expect(cb.remainingCooldownMs()).toBe(800);
    now = 5000;
    expect(cb.remainingCooldownMs()).toBe(0);
  });
});
