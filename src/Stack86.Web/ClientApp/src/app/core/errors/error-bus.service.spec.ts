import { describe, expect, it } from 'vitest';
import { ErrorBus, type ReportedError } from './error-bus.service';

describe('ErrorBus', () => {
  it('starts with no error', () => {
    const bus = new ErrorBus();
    expect(bus.latest()).toBeNull();
  });

  it('exposes the latest reported error via signal', () => {
    const bus = new ErrorBus();
    const err: ReportedError = { title: 'Boom', detail: 'Something exploded', traceId: 't-1' };
    bus.report(err);
    expect(bus.latest()).toEqual(err);
  });

  it('overwrites the previous error on subsequent reports', () => {
    const bus = new ErrorBus();
    bus.report({ title: 'A', detail: 'first' });
    bus.report({ title: 'B', detail: 'second' });
    expect(bus.latest()?.title).toBe('B');
  });

  it('clears via dismiss()', () => {
    const bus = new ErrorBus();
    bus.report({ title: 'A', detail: 'x' });
    bus.dismiss();
    expect(bus.latest()).toBeNull();
  });

  it('dismiss is a no-op when nothing reported', () => {
    const bus = new ErrorBus();
    expect(() => bus.dismiss()).not.toThrow();
    expect(bus.latest()).toBeNull();
  });
});
