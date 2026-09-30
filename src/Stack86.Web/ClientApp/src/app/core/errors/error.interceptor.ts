import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { ErrorBus } from './error-bus.service';

/**
 * Surfaces HTTP errors to the global ErrorBus. Domain-specific errors (validation, auth)
 * are still re-thrown so callers can handle them locally.
 */
export const errorInterceptor: HttpInterceptorFn = (
  request: HttpRequest<unknown>,
  next: HttpHandlerFn,
): Observable<HttpEvent<unknown>> => {
  const bus = inject(ErrorBus);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        // Surface only system errors globally; let component-level handle 4xx auth/validation.
        if (error.status === 0 || error.status >= 500) {
          bus.report({
            title: 'Network or server error',
            detail: extractDetail(error) ?? `Request failed (${error.status}).`,
            traceId: extractTraceId(error),
          });
        }
      }
      return throwError(() => error);
    }),
  );
};

interface ProblemDetailsLike {
  readonly title?: string;
  readonly detail?: string;
  readonly traceId?: string;
  readonly errors?: Record<string, readonly string[]>;
}

function isProblemDetailsLike(value: unknown): value is ProblemDetailsLike {
  return typeof value === 'object' && value !== null;
}

function extractDetail(response: HttpErrorResponse): string | undefined {
  const body: unknown = response.error;
  if (typeof body === 'string') {
    return body;
  }
  if (isProblemDetailsLike(body)) {
    return body.detail ?? body.title;
  }
  return undefined;
}

function extractTraceId(response: HttpErrorResponse): string | undefined {
  const body: unknown = response.error;
  if (isProblemDetailsLike(body)) {
    return body.traceId;
  }
  return undefined;
}
