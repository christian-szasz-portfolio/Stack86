import { describe, expect, it } from 'vitest';
import { appConfig } from './app.config';

describe('appConfig', () => {
  it('exposes a non-empty provider array', () => {
    expect(Array.isArray(appConfig.providers)).toBe(true);
    expect(appConfig.providers.length).toBeGreaterThan(0);
  });
});
