import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { MonacoEditorService } from './monaco-editor.service';
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
});
