/**
 * Failure-counting circuit breaker. Three states:
 *  - Closed  : calls flow normally; consecutive failures are counted
 *  - Open    : calls are blocked until cooldown elapses
 *  - HalfOpen: a single trial call is permitted; success closes, failure re-opens
 */
export enum CircuitBreakerState {
  Closed = 'closed',
  Open = 'open',
  HalfOpen = 'half-open',
}

export interface CircuitBreakerOptions {
  /** Consecutive failures required to trip the breaker. */
  readonly failureThreshold: number;
  /** Milliseconds the breaker stays Open before transitioning to HalfOpen. */
  readonly cooldownMs: number;
  /** Optional clock injection for testability. */
  readonly now?: () => number;
}

export class CircuitBreaker {
  private state: CircuitBreakerState = CircuitBreakerState.Closed;
  private failureCount: number = 0;
  private openedAt: number = 0;
  private readonly threshold: number;
  private readonly cooldownMs: number;
  private readonly now: () => number;

  public constructor(options: CircuitBreakerOptions) {
    this.threshold = options.failureThreshold;
    this.cooldownMs = options.cooldownMs;
    this.now = options.now ?? (() => Date.now());
  }

  public getState(): CircuitBreakerState {
    if (this.state === CircuitBreakerState.Open && this.now() - this.openedAt >= this.cooldownMs) {
      this.state = CircuitBreakerState.HalfOpen;
    }
    return this.state;
  }

  public canExecute(): boolean {
    return this.getState() !== CircuitBreakerState.Open;
  }

  public remainingCooldownMs(): number {
    if (this.state !== CircuitBreakerState.Open) {
      return 0;
    }
    return Math.max(0, this.cooldownMs - (this.now() - this.openedAt));
  }

  public recordSuccess(): void {
    this.failureCount = 0;
    this.state = CircuitBreakerState.Closed;
  }

  public recordFailure(): void {
    if (this.state === CircuitBreakerState.HalfOpen) {
      this.trip();
      return;
    }
    this.failureCount += 1;
    if (this.failureCount >= this.threshold) {
      this.trip();
    }
  }

  private trip(): void {
    this.state = CircuitBreakerState.Open;
    this.openedAt = this.now();
    this.failureCount = 0;
  }
}
