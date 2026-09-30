/**
 * Keeps this demo's browser storage in one place instead of scattering keys across the visitor's
 * localStorage. Every key the app reads or writes is stored under a single prefix, so a person
 * inspecting their storage sees one contained group rather than a dozen loose entries.
 *
 * The app keeps using `localStorage` exactly as before; the prefix is added and stripped
 * transparently, and `length`, `key()` and `clear()` report only this app's own keys, so code
 * that iterates storage still sees the keys it wrote.
 */

/** Wraps a backing store so every key lives under `namespace`. Exposed for testing. */
export function createNamespacedStorage(namespace: string, backing: Storage): Storage {
  const ownKeys = (): string[] => {
    const keys: string[] = [];
    for (let i = 0; i < backing.length; i++) {
      const key = backing.key(i);
      if (key !== null && key.startsWith(namespace)) {
        keys.push(key);
      }
    }
    return keys;
  };

  return {
    get length(): number {
      return ownKeys().length;
    },
    clear(): void {
      ownKeys().forEach((key) => backing.removeItem(key));
    },
    getItem(key: string): string | null {
      return backing.getItem(namespace + key);
    },
    key(index: number): string | null {
      const own = ownKeys()[index];
      return own === undefined ? null : own.slice(namespace.length);
    },
    removeItem(key: string): void {
      backing.removeItem(namespace + key);
    },
    setItem(key: string, value: string): void {
      backing.setItem(namespace + key, value);
    },
  };
}

/**
 * Installs the namespace over the real `window.localStorage`, once, at startup. A no-op off the
 * browser or where storage is blocked or cannot be redefined, so the app degrades to the plain
 * store rather than failing.
 */
export function installNamespacedStorage(namespace: string): void {
  if (typeof window === 'undefined') {
    return;
  }

  let backing: Storage;
  try {
    backing = window.localStorage;
  } catch {
    // Storage blocked (private mode, disabled): nothing to namespace.
    return;
  }
  if (!backing) {
    return;
  }

  const namespaced = createNamespacedStorage(namespace, backing);
  try {
    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      get: () => namespaced,
    });
  } catch {
    // Some environments make localStorage non-configurable; keep the plain store then.
  }
}
