import { describe, expect, it } from 'vitest';
import { SAMPLE_PROGRAMS } from './sample-programs';

describe('assembler sample-programs', () => {
  it('exports a non-empty list of programs', () => {
    expect(SAMPLE_PROGRAMS.length).toBeGreaterThan(0);
  });

  it('every program has a non-empty name and source', () => {
    for (const p of SAMPLE_PROGRAMS) {
      expect(p.name.trim().length).toBeGreaterThan(0);
      expect(p.source.length).toBeGreaterThan(0);
    }
  });

  it('program names are unique', () => {
    const names = SAMPLE_PROGRAMS.map((p) => p.name);
    expect(new Set(names).size).toBe(names.length);
  });
});
