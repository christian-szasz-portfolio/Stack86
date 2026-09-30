import { describe, expect, it } from 'vitest';
import { LANGUAGE_OPTIONS, SupportedLanguage } from '../../../core/compiler/compiler.models';
import {
  C_SAMPLE_PROGRAMS,
  DEFAULT_SAMPLE_INDEX,
  SAMPLE_REGISTRY,
  getDefaultSample,
  getSamplesForLanguage,
} from './sample-programs';

const ENABLED_LANGUAGES = LANGUAGE_OPTIONS.filter((l) => l.enabled).map((l) => l.id);

describe('sample-programs', () => {
  it('exposes a non-empty C program list', () => {
    expect(C_SAMPLE_PROGRAMS.length).toBeGreaterThan(0);
    for (const prog of C_SAMPLE_PROGRAMS) {
      expect(prog.name).toBeTruthy();
      expect(prog.files.length).toBeGreaterThan(0);
      for (const file of prog.files) {
        expect(file.name).toBeTruthy();
        expect(typeof file.content).toBe('string');
      }
    }
  });

  it('default sample index is in range', () => {
    expect(DEFAULT_SAMPLE_INDEX).toBeGreaterThanOrEqual(0);
    expect(DEFAULT_SAMPLE_INDEX).toBeLessThan(C_SAMPLE_PROGRAMS.length);
  });

  it('registry has an entry for every demo-enabled language', () => {
    for (const lang of ENABLED_LANGUAGES) {
      const samples = SAMPLE_REGISTRY[lang];
      expect(samples).toBeDefined();
      expect(Array.isArray(samples)).toBe(true);
    }
  });

  describe('getSamplesForLanguage', () => {
    it('returns the C list for SupportedLanguage.C', () => {
      expect(getSamplesForLanguage(SupportedLanguage.C)).toBe(C_SAMPLE_PROGRAMS);
    });

    it('returns a non-empty array for every demo-enabled language', () => {
      for (const lang of ENABLED_LANGUAGES) {
        expect(getSamplesForLanguage(lang).length).toBeGreaterThan(0);
      }
    });

    it('returns [] for an unknown language', () => {
      expect(getSamplesForLanguage('klingon' as SupportedLanguage)).toEqual([]);
    });
  });

  describe('getDefaultSample', () => {
    it('returns the first program for each demo-enabled language', () => {
      for (const lang of ENABLED_LANGUAGES) {
        const sample = getDefaultSample(lang);
        expect(sample).toBeDefined();
        expect(sample).toBe(getSamplesForLanguage(lang)[0]);
      }
    });

    it('returns undefined for an unknown language', () => {
      expect(getDefaultSample('klingon' as SupportedLanguage)).toBeUndefined();
    });
  });

  describe('multi-file programs', () => {
    it('A* pathfinding sample contains main.c, astar.h and astar.c', () => {
      const astar = C_SAMPLE_PROGRAMS.find((p) => p.name === 'A* Pathfinding');
      expect(astar).toBeDefined();
      const names = astar!.files.map((f) => f.name).sort();
      expect(names).toEqual(['astar.c', 'astar.h', 'main.c']);
    });
  });
});
