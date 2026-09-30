/**
 * Wraps an `AbortController` with an idle-timeout. Each call to `bumpActivity()`
 * resets the timer; if the timer expires the controller is aborted with reason `'idle'`.
 */
export interface IdleTimeoutOptions {
  readonly idleMs: number;
  /** Optional schedulers for testability. */
  readonly setTimer?: (cb: () => void, ms: number) => number;
  readonly clearTimer?: (handle: number) => void;
}

export class IdleTimeoutController {
  private readonly controller: AbortController = new AbortController();
  private readonly idleMs: number;
  private readonly setTimer: (cb: () => void, ms: number) => number;
  private readonly clearTimer: (handle: number) => void;
  private timer: number | null = null;
  private disposed: boolean = false;

  public constructor(options: IdleTimeoutOptions) {
    this.idleMs = options.idleMs;
    this.setTimer = options.setTimer ?? IdleTimeoutController.defaultSetTimer;
    this.clearTimer = options.clearTimer ?? IdleTimeoutController.defaultClearTimer;
  }

  public get signal(): AbortSignal {
    return this.controller.signal;
  }

  public start(): void {
    this.bumpActivity();
  }

  public bumpActivity(): void {
    if (this.disposed) {
      return;
    }
    this.cancelTimer();
    this.timer = this.setTimer(() => this.controller.abort('idle'), this.idleMs);
  }

  public abort(reason?: unknown): void {
    this.cancelTimer();
    this.controller.abort(reason);
  }

  public dispose(): void {
    this.disposed = true;
    this.cancelTimer();
  }

  private cancelTimer(): void {
    if (this.timer !== null) {
      this.clearTimer(this.timer);
      this.timer = null;
    }
  }

  private static defaultSetTimer(cb: () => void, ms: number): number {
    return setTimeout(cb, ms) as unknown as number;
  }

  private static defaultClearTimer(handle: number): void {
    clearTimeout(handle);
  }
}
