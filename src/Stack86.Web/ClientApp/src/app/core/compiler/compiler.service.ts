import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subscriber } from 'rxjs';
import { CompileRequest, CompileResponse, CompileStreamEvent } from './compiler.models';
import { CircuitBreaker, CircuitOpenError, IdleTimeoutController, RetryPolicy } from '../resilience';
import { TerminalCompileError } from './terminal-compile-error';

@Service()
export class CompilerService {
  private static readonly idleTimeoutMs = 30_000;
  private static readonly breaker: CircuitBreaker = new CircuitBreaker({
    failureThreshold: 4,
    cooldownMs: 15_000,
  });
  private static readonly retryPolicy: RetryPolicy = new RetryPolicy({
    maxAttempts: 3,
    baseDelayMs: 300,
    maxDelayMs: 4_000,
    isTransient: (err: unknown): boolean => {
      if (err instanceof TerminalCompileError) {
        return false;
      }
      if (err instanceof DOMException && err.name === 'AbortError') {
        const reason: unknown = (err as DOMException & { cause?: unknown }).cause;
        return reason === 'idle';
      }
      return err instanceof Error;
    },
  });

  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/Compiler/compile';
  private readonly streamUrl = '/api/Compiler/compileStream';

  public compile(language: string, files: Record<string, string>): Observable<CompileResponse> {
    const request: CompileRequest = { language, files };
    return this.http.post<CompileResponse>(this.apiUrl, request);
  }

  public compileStream(language: string, files: Record<string, string>): Observable<CompileStreamEvent> {
    const request: CompileRequest = { language, files };

    return new Observable<CompileStreamEvent>((subscriber) => {
      const breaker = CompilerService.breaker;
      if (!breaker.canExecute()) {
        subscriber.error(new CircuitOpenError(breaker.remainingCooldownMs()));
        return;
      }

      const outer = new AbortController();
      let cancelled = false;
      let activeIdle: IdleTimeoutController | null = null;

      const work = async (): Promise<void> => {
        try {
          await CompilerService.retryPolicy.executeAsync(() =>
            this.runAttempt(request, subscriber, outer.signal, (idle) => {
              activeIdle = idle;
            }),
          );
          breaker.recordSuccess();
          subscriber.complete();
        } catch (err) {
          if (cancelled) {
            subscriber.complete();
            return;
          }
          breaker.recordFailure();
          const message = err instanceof Error ? err.message : 'Compilation request failed';
          subscriber.error(err instanceof Error ? err : new Error(message));
        }
      };

      void work();

      return () => {
        cancelled = true;
        activeIdle?.dispose();
        outer.abort();
      };
    });
  }

  private async runAttempt(
    request: CompileRequest,
    subscriber: Subscriber<CompileStreamEvent>,
    outerSignal: AbortSignal,
    setIdle: (idle: IdleTimeoutController) => void,
  ): Promise<void> {
    const idle = new IdleTimeoutController({ idleMs: CompilerService.idleTimeoutMs });
    setIdle(idle);
    idle.start();

    const onOuterAbort = (): void => idle.abort('cancelled');
    outerSignal.addEventListener('abort', onOuterAbort, { once: true });

    try {
      const response = await fetch(this.streamUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
        signal: idle.signal,
      });

      if (!response.ok) {
        if (response.status >= 400 && response.status < 500) {
          throw new TerminalCompileError(`Compilation request failed (${response.status})`);
        }
        throw new Error(`Compilation request failed (${response.status})`);
      }

      if (!response.body) {
        throw new Error('Compilation response had no body');
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      for (;;) {
        const { done, value } = await reader.read();
        if (done) {
          break;
        }
        idle.bumpActivity();

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop()!;

        for (const line of lines) {
          if (line.trim()) {
            this.dispatchEvent(JSON.parse(line) as CompileStreamEvent, subscriber);
          }
        }
      }

      if (buffer.trim()) {
        this.dispatchEvent(JSON.parse(buffer) as CompileStreamEvent, subscriber);
      }
    } finally {
      outerSignal.removeEventListener('abort', onOuterAbort);
      idle.dispose();
    }
  }

  private dispatchEvent(event: CompileStreamEvent, subscriber: Subscriber<CompileStreamEvent>): void {
    if (event.type === 'heartbeat') {
      return;
    }
    subscriber.next(event);
  }
}
