/**
 * Retries an async operation with exponential backoff and full jitter.
 * Only errors matching `isTransient` are retried; everything else propagates immediately.
 */
export interface RetryPolicyOptions {
  readonly maxAttempts: number;
  readonly baseDelayMs: number;
  readonly maxDelayMs: number;
  readonly isTransient: (error: unknown) => boolean;
  /** Optional injection for tests; defaults to setTimeout. */
  readonly delay?: (ms: number) => Promise<void>;
  /** Optional injection for tests; defaults to Math.random. */
  readonly random?: () => number;
}

export class RetryPolicy {
  private readonly maxAttempts: number;
  private readonly baseDelayMs: number;
  private readonly maxDelayMs: number;
  private readonly isTransient: (error: unknown) => boolean;
  private readonly delay: (ms: number) => Promise<void>;
  private readonly random: () => number;

  public constructor(options: RetryPolicyOptions) {
    this.maxAttempts = options.maxAttempts;
    this.baseDelayMs = options.baseDelayMs;
    this.maxDelayMs = options.maxDelayMs;
    this.isTransient = options.isTransient;
    this.delay = options.delay ?? RetryPolicy.defaultDelay;
    this.random = options.random ?? Math.random;
  }

  public async executeAsync<T>(operation: (attempt: number) => Promise<T>): Promise<T> {
    let attempt = 0;
    for (;;) {
      attempt += 1;
      try {
        return await operation(attempt);
      } catch (error) {
        if (attempt >= this.maxAttempts || !this.isTransient(error)) {
          throw error;
        }
        await this.delay(this.computeBackoffMs(attempt));
      }
    }
  }

  private computeBackoffMs(attempt: number): number {
    const exp = Math.min(this.maxDelayMs, this.baseDelayMs * 2 ** (attempt - 1));
    return Math.floor(this.random() * exp);
  }

  private static defaultDelay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}
