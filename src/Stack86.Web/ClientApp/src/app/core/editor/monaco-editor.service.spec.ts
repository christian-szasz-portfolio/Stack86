import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { EDITOR_FONT, MonacoEditorService } from './monaco-editor.service';
import { MonacoTestUtility } from '../../testing/test-helpers';

describe('MonacoEditorService', () => {
  let service: MonacoEditorService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MonacoEditorService);
  });

  afterEach(() => vi.restoreAllMocks());

  it('dispose delegates to the editor instance', () => {
    const editor = MonacoTestUtility.createEditor();
    service.dispose(editor as unknown as Parameters<MonacoEditorService['dispose']>[0]);
    expect(editor.dispose).toHaveBeenCalledTimes(1);
  });

  it('loadMonaco memoizes the result across calls', () => {
    // Force a deterministic result by stubbing the internal field via reflection
    // — avoids actually loading monaco-editor in jsdom.
    const ref = service as unknown as { monacoPromise: Promise<unknown> | null };
    const stub = Promise.resolve({ stub: true });
    ref.monacoPromise = stub;

    const first = service.loadMonaco();
    const second = service.loadMonaco();
    expect(first).toBe(stub);
    expect(second).toBe(stub);
  });

  describe('loadEditorFont', () => {
    // jsdom has no Font Loading API, so each test puts one on the document and takes it off again
    const withFonts = (load: () => Promise<unknown>): void => {
      Object.defineProperty(document, 'fonts', { value: { load }, configurable: true });
    };

    afterEach(() => {
      Reflect.deleteProperty(document, 'fonts');
    });

    it('asks the browser for the self-hosted editor face', async () => {
      const load = vi.fn().mockResolvedValue([]);
      withFonts(load);

      await service.loadEditorFont();

      expect(load).toHaveBeenCalledWith(EDITOR_FONT);
    });

    it('still resolves when the font fails to load', async () => {
      withFonts(vi.fn().mockRejectedValue(new Error('offline')));

      await expect(service.loadEditorFont()).resolves.toBeUndefined();
    });

    it('resolves at once in a browser without the Font Loading API', async () => {
      await expect(service.loadEditorFont()).resolves.toBeUndefined();
    });
  });
});
