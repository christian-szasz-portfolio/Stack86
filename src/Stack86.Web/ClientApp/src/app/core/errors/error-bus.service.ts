import { Service, signal } from '@angular/core';

export interface ReportedError {
  readonly title: string;
  readonly detail: string;
  readonly traceId?: string;
}

/**
 * In-memory pub/sub bus for global errors surfaced by interceptors and the global error handler.
 * Components subscribe via the `latest` signal to render a friendly modal.
 */
@Service()
export class ErrorBus {
  private readonly latestSignal = signal<ReportedError | null>(null);

  public readonly latest = this.latestSignal.asReadonly();

  public report(error: ReportedError): void {
    this.latestSignal.set(error);
  }

  public dismiss(): void {
    this.latestSignal.set(null);
  }
}
