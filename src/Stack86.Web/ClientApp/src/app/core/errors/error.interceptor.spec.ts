import {
  HttpClient,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { errorInterceptor } from './error.interceptor';
import { ErrorBus } from './error-bus.service';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let bus: ErrorBus;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    bus = TestBed.inject(ErrorBus);
  });

  afterEach(() => controller.verify());

  it('reports 5xx responses to the ErrorBus', () => {
    let err: HttpErrorResponse | null = null;
    http.get('/api/data').subscribe({ error: (e: HttpErrorResponse) => (err = e) });

    controller.expectOne('/api/data').flush(
      { title: 'Boom', detail: 'thing exploded', traceId: 'tr-1' },
      { status: 500, statusText: 'Server Error' },
    );

    expect(err?.status).toBe(500);
    expect(bus.latest()?.detail).toBe('thing exploded');
    expect(bus.latest()?.traceId).toBe('tr-1');
    expect(bus.latest()?.title).toBe('Network or server error');
  });

  it('reports network errors (status 0)', () => {
    http.get('/api/data').subscribe({ error: () => undefined });
    controller
      .expectOne('/api/data')
      .error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });
    expect(bus.latest()).not.toBeNull();
  });

  it('falls back to title when detail is missing', () => {
    http.get('/api/data').subscribe({ error: () => undefined });
    controller.expectOne('/api/data').flush(
      { title: 'Title only' },
      { status: 503, statusText: 'Unavailable' },
    );
    expect(bus.latest()?.detail).toBe('Title only');
  });

  it('uses generic detail when no body is present', () => {
    http.get('/api/data').subscribe({ error: () => undefined });
    controller.expectOne('/api/data').flush(null, { status: 502, statusText: 'Bad Gateway' });
    expect(bus.latest()?.detail).toContain('502');
  });

  it('handles plain string error bodies', () => {
    http.get('/api/data').subscribe({ error: () => undefined });
    controller.expectOne('/api/data').flush('raw-error', { status: 500, statusText: 'X' });
    expect(bus.latest()?.detail).toBe('raw-error');
  });

  it('does NOT report 4xx errors (handled by callers)', () => {
    http.get('/api/data').subscribe({ error: () => undefined });
    controller.expectOne('/api/data').flush({}, { status: 400, statusText: 'Bad Request' });
    expect(bus.latest()).toBeNull();
  });

  it('does NOT report 401', () => {
    http.get('/api/data').subscribe({ error: () => undefined });
    controller.expectOne('/api/data').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(bus.latest()).toBeNull();
  });

  it('does NOT touch successful responses', () => {
    let value: unknown = null;
    http.get('/api/data').subscribe((v) => (value = v));
    controller.expectOne('/api/data').flush({ ok: true });
    expect(value).toEqual({ ok: true });
    expect(bus.latest()).toBeNull();
  });

  it('always re-throws the error', () => {
    let err: HttpErrorResponse | null = null;
    http.get('/api/data').subscribe({ error: (e: HttpErrorResponse) => (err = e) });
    controller.expectOne('/api/data').flush({}, { status: 500, statusText: 'X' });
    expect(err).toBeInstanceOf(HttpErrorResponse);
  });
});
