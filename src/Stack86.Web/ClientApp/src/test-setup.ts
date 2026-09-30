import '@angular/compiler';
import '@analogjs/vitest-angular/setup-snapshots';
import { setupTestBed } from '@analogjs/vitest-angular/setup-testbed';

// jsdom does not implement ResizeObserver. Components that observe their own size — the data-flow
// diagram, and Monaco via automaticLayout — threw on construction without it, which surfaced as
// unhandled errors and made `npm test` exit non-zero even though every test passed.
if (!('ResizeObserver' in globalThis)) {
  class ResizeObserverStub implements ResizeObserver {
    public observe(): void {
      // jsdom performs no layout, so there is never anything to report.
    }

    public unobserve(): void {
      // no-op
    }

    public disconnect(): void {
      // no-op
    }
  }

  globalThis.ResizeObserver = ResizeObserverStub;
}

setupTestBed();
