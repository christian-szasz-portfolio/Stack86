import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { GlobalErrorHandler } from './global-error.handler';
import { ErrorBus } from './error-bus.service';

describe('GlobalErrorHandler', () => {
  let handler: GlobalErrorHandler;
  let bus: ErrorBus;
  let consoleSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [GlobalErrorHandler],
    });
    handler = TestBed.inject(GlobalErrorHandler);
    bus = TestBed.inject(ErrorBus);
    consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  afterEach(() => {
    consoleSpy.mockRestore();
  });

  it('reports plain Error messages to the bus', () => {
    handler.handleError(new Error('Kaboom'));
    expect(bus.latest()?.title).toBe('Unexpected error');
    expect(bus.latest()?.detail).toBe('Kaboom');
  });

  it('reports non-Error values via String() coercion', () => {
    handler.handleError('something broke');
    expect(bus.latest()?.detail).toBe('something broke');
  });

  it('falls back to a friendly message when the error has no message', () => {
    handler.handleError(new Error(''));
    expect(bus.latest()?.detail).toBe('Something went wrong. Please try again.');
  });

  it('always logs to the console', () => {
    const err = new Error('x');
    handler.handleError(err);
    expect(consoleSpy).toHaveBeenCalledWith(err);
  });

  it('ignores HttpErrorResponse (handled by errorInterceptor)', () => {
    const err = new HttpErrorResponse({ status: 500 });
    handler.handleError(err);
    expect(bus.latest()).toBeNull();
    // Still no console call for HTTP either since it returns early.
    expect(consoleSpy).not.toHaveBeenCalled();
  });
});
