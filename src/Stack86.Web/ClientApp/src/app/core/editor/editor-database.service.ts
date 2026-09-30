import { Service } from '@angular/core';

const DB_NAME = 'stack86';
const DB_VERSION = 1;
const STORE_NAME = 'editor-contents';

/**
 * Persists editor contents using IndexedDB, storing text as binary blobs.
 */
@Service()
export class EditorDatabaseService {
  private dbPromise: Promise<IDBDatabase> | null = null;
  private readonly encoder = new TextEncoder();
  private readonly decoder = new TextDecoder();

  public async save(key: string, content: string): Promise<void> {
    const db = await this.open();
    const binary = this.encoder.encode(content);
    return new Promise((resolve, reject) => {
      const tx = db.transaction(STORE_NAME, 'readwrite');
      tx.objectStore(STORE_NAME).put(binary, key);
      tx.oncomplete = () => resolve();
      tx.onerror = () => reject(tx.error);
    });
  }

  public async load(key: string): Promise<string | null> {
    const db = await this.open();
    return new Promise((resolve, reject) => {
      const tx = db.transaction(STORE_NAME, 'readonly');
      const request = tx.objectStore(STORE_NAME).get(key);
      request.onsuccess = () => {
        const result = request.result as Uint8Array | undefined;
        resolve(result ? this.decoder.decode(result) : null);
      };
      request.onerror = () => reject(request.error);
    });
  }

  private open(): Promise<IDBDatabase> {
    if (!this.dbPromise) {
      this.dbPromise = new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);
        request.onupgradeneeded = () => {
          const db = request.result;
          if (!db.objectStoreNames.contains(STORE_NAME)) {
            db.createObjectStore(STORE_NAME);
          }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
      });
    }
    return this.dbPromise;
  }
}
