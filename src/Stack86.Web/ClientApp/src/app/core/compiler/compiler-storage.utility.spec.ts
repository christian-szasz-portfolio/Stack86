import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { CompilerStorageUtility } from './compiler-storage.utility';
import { SupportedLanguage } from './compiler.models';

describe('CompilerStorageUtility', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  describe('language', () => {
    it('round-trips a known language', () => {
      CompilerStorageUtility.saveLanguage(SupportedLanguage.TypeScript);
      expect(CompilerStorageUtility.loadLanguage()).toBe(SupportedLanguage.TypeScript);
    });

    it('returns null when nothing stored', () => {
      expect(CompilerStorageUtility.loadLanguage()).toBeNull();
    });

    it('returns null when an unknown language is in storage', () => {
      localStorage.setItem('compiler.selectedLanguage', 'klingon');
      expect(CompilerStorageUtility.loadLanguage()).toBeNull();
    });
  });

  describe('sample', () => {
    it('round-trips an index for the same language', () => {
      CompilerStorageUtility.saveSample(SupportedLanguage.C, 3);
      expect(CompilerStorageUtility.loadSample(SupportedLanguage.C)).toBe(3);
    });

    it('returns null when language does not match', () => {
      CompilerStorageUtility.saveSample(SupportedLanguage.C, 3);
      expect(CompilerStorageUtility.loadSample(SupportedLanguage.Cpp)).toBeNull();
    });

    it('returns null when nothing stored', () => {
      expect(CompilerStorageUtility.loadSample(SupportedLanguage.C)).toBeNull();
    });

    it('returns null when index portion is malformed', () => {
      localStorage.setItem('compiler.selectedSample', `${SupportedLanguage.C}:not-a-number`);
      expect(CompilerStorageUtility.loadSample(SupportedLanguage.C)).toBeNull();
    });

    it('returns 0 when index is "0"', () => {
      CompilerStorageUtility.saveSample(SupportedLanguage.C, 0);
      expect(CompilerStorageUtility.loadSample(SupportedLanguage.C)).toBe(0);
    });
  });
});
