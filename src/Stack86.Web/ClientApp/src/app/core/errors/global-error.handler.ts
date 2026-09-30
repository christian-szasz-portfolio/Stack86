import { ErrorHandler, Service, NgZone, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ErrorBus } from './error-bus.service';

/**
 * Global Angular error handler. Routes uncaught errors (template, effect, etc.) to the
 * ErrorBus so the user sees a friendly modal. HTTP errors are intentionally ignored here
 * because they're already surfaced by errorInterceptor.
 */
@Service({ autoProvided: false })
export class GlobalErrorHandler implements ErrorHandler {
  private readonly bus = inject(ErrorBus);
  private readonly zone = inject(NgZone);

  public handleError(error: unknown): void {
    if (error instanceof HttpErrorResponse) {
      // Already handled by errorInterceptor.
      return;
    }

    const message = error instanceof Error ? error.message : String(error);
    this.zone.run(() => {
      this.bus.report({
        title: 'Unexpected error',
        detail: message || 'Something went wrong. Please try again.',
      });
    });

    // Still log so developers see the stack in DevTools.
    console.error(error);
  }
}
