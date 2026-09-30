import { createNamespacedStorage } from './namespaced-storage.utility';

/** A plain in-memory Storage, so the tests never touch the real localStorage. */
function memoryStorage(): Storage {
  const map = new Map<string, string>();
  return {
    get length(): number {
      return map.size;
    },
    clear(): void {
      map.clear();
    },
    getItem(key: string): string | null {
      return map.has(key) ? (map.get(key) ?? null) : null;
    },
    key(index: number): string | null {
      return Array.from(map.keys())[index] ?? null;
    },
    removeItem(key: string): void {
      map.delete(key);
    },
    setItem(key: string, value: string): void {
      map.set(key, String(value));
    },
  } as Storage;
}

describe('namespaced-storage.utility', () => {
  const NS = 'stack86-demo:';

  it('stores every key under the namespace in the backing store', () => {
    const backing = memoryStorage();
    const store = createNamespacedStorage(NS, backing);

    store.setItem('compiler.selectedLanguage', 'c');

    expect(backing.getItem('stack86-demo:compiler.selectedLanguage')).toBe('c');
    expect(backing.getItem('compiler.selectedLanguage')).toBeNull();
  });

  it('reads its own keys back without the prefix', () => {
    const backing = memoryStorage();
    const store = createNamespacedStorage(NS, backing);

    store.setItem('stack86_registers_showHex', 'true');

    expect(store.getItem('stack86_registers_showHex')).toBe('true');
  });

  it('counts and enumerates only its own keys, and hands them back stripped', () => {
    const backing = memoryStorage();
    backing.setItem('someone-elses-key', 'x');
    const store = createNamespacedStorage(NS, backing);

    store.setItem('compiler.selectedSample', 'hello');
    store.setItem('stack86_stack_showBinary', 'false');

    expect(store.length).toBe(2);
    const keys = [store.key(0), store.key(1)];
    expect(keys).toContain('compiler.selectedSample');
    expect(keys).toContain('stack86_stack_showBinary');
    expect(keys).not.toContain('someone-elses-key');
  });

  it('clears only its own keys, leaving foreign ones', () => {
    const backing = memoryStorage();
    backing.setItem('extension-key', 'keep-me');
    const store = createNamespacedStorage(NS, backing);
    store.setItem('compiler.selectedLanguage', 'c');

    store.clear();

    expect(store.length).toBe(0);
    expect(backing.getItem('extension-key')).toBe('keep-me');
  });
});
