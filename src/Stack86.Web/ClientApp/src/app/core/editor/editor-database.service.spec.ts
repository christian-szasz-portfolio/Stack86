import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { EditorDatabaseService } from './editor-database.service';

interface FakeRequest<T = unknown> {
  result: T;
  error: unknown;
  onsuccess: (() => void) | null;
  onerror: (() => void) | null;
}

interface FakeStore {
  put: ReturnType<typeof vi.fn>;
  get: ReturnType<typeof vi.fn>;
}

interface FakeTransaction {
  objectStore: ReturnType<typeof vi.fn>;
  oncomplete: (() => void) | null;
  onerror: (() => void) | null;
  error: unknown;
}

interface FakeDatabase {
  transaction: ReturnType<typeof vi.fn>;
  objectStoreNames: { contains: () => boolean };
  createObjectStore: ReturnType<typeof vi.fn>;
}

interface FakeOpenRequest {
  result: FakeDatabase;
  error: unknown;
  onsuccess: (() => void) | null;
  onerror: (() => void) | null;
  onupgradeneeded: (() => void) | null;
}

describe('EditorDatabaseService', () => {
  let service: EditorDatabaseService;
  let storedRequest: FakeRequest<Uint8Array | undefined>;
  let store: FakeStore;
  let tx: FakeTransaction;
  let db: FakeDatabase;
  let openReq: FakeOpenRequest;
  let originalIndexedDb: IDBFactory;

  beforeEach(() => {
    storedRequest = { result: undefined, error: null, onsuccess: null, onerror: null };
    store = {
      put: vi.fn((value: Uint8Array, _key: string) => {
        storedRequest.result = value;
        return storedRequest;
      }),
      get: vi.fn(() => storedRequest),
    };
    tx = {
      objectStore: vi.fn(() => store),
      oncomplete: null,
      onerror: null,
      error: null,
    };
    db = {
      transaction: vi.fn(() => {
        // Run a microtask to invoke success callbacks so the service's
        // promise resolves naturally when awaited.
        queueMicrotask(() => {
          tx.oncomplete?.();
          storedRequest.onsuccess?.();
        });
        return tx;
      }),
      objectStoreNames: { contains: () => true },
      createObjectStore: vi.fn(),
    };
    openReq = {
      result: db,
      error: null,
      onsuccess: null,
      onerror: null,
      onupgradeneeded: null,
    };

    originalIndexedDb = (globalThis as unknown as { indexedDB: IDBFactory }).indexedDB;
    (globalThis as unknown as { indexedDB: { open: ReturnType<typeof vi.fn> } }).indexedDB = {
      open: vi.fn(() => {
        queueMicrotask(() => openReq.onsuccess?.());
        return openReq;
      }),
    };

    TestBed.configureTestingModule({});
    service = TestBed.inject(EditorDatabaseService);
  });

  afterEach(() => {
    (globalThis as unknown as { indexedDB: IDBFactory }).indexedDB = originalIndexedDb;
    vi.restoreAllMocks();
  });

  it('save encodes content as a Uint8Array and stores under the key', async () => {
    await service.save('k1', 'hello');
    expect(store.put).toHaveBeenCalledTimes(1);
    const [bytes, key] = store.put.mock.calls[0];
    expect(key).toBe('k1');
    expect(bytes).toBeInstanceOf(Uint8Array);
    expect(new TextDecoder().decode(bytes as Uint8Array)).toBe('hello');
  });

  it('load decodes stored bytes back to a string', async () => {
    storedRequest.result = new TextEncoder().encode('hello');
    expect(await service.load('k1')).toBe('hello');
  });

  it('load returns null when no entry exists', async () => {
    storedRequest.result = undefined;
    expect(await service.load('missing')).toBeNull();
  });

  it('rejects when the get request errors', async () => {
    store.get = vi.fn(() => {
      const r = { ...storedRequest };
      queueMicrotask(() => {
        r.error = new Error('boom');
        r.onerror?.();
      });
      return r;
    });
    db.transaction = vi.fn(() => tx);

    await expect(service.load('x')).rejects.toBeTruthy();
  });

  it('reuses a single open() request across calls', async () => {
    const open = (globalThis as unknown as { indexedDB: { open: ReturnType<typeof vi.fn> } })
      .indexedDB.open;
    await service.save('a', 'one');
    await service.save('b', 'two');
    expect(open).toHaveBeenCalledTimes(1);
  });

  it('creates the object store on upgrade when missing', async () => {
    db.objectStoreNames = { contains: () => false };
    const upgradeCall = service.save('first', 'value');
    queueMicrotask(() => openReq.onupgradeneeded?.());
    await upgradeCall;
    expect(db.createObjectStore).toHaveBeenCalledWith('editor-contents');
  });
});
