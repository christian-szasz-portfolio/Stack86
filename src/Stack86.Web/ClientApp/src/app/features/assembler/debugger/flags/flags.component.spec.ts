import { describe, it, expect } from 'vitest';
import { CpuFlags } from '@core/emulator/cpu/cpu.model';

/**
 * Tests the flag display logic used by FlagsComponent.
 * Mirrors the computed signal transformation without Angular DI.
 */

interface FlagEntry {
  name: string;
  set: boolean;
}

function buildFlagEntries(flags: CpuFlags): FlagEntry[] {
  return [
    { name: 'ZF', set: flags.zero },
    { name: 'CF', set: flags.carry },
    { name: 'SF', set: flags.sign },
    { name: 'OF', set: flags.overflow },
  ];
}

describe('FlagsComponent logic', () => {
  it('should produce 4 flag entries', () => {
    const flags: CpuFlags = { zero: false, carry: false, sign: false, overflow: false };
    expect(buildFlagEntries(flags)).toHaveLength(4);
  });

  it('should have correct flag names', () => {
    const flags: CpuFlags = { zero: false, carry: false, sign: false, overflow: false };
    const names = buildFlagEntries(flags).map(f => f.name);
    expect(names).toEqual(['ZF', 'CF', 'SF', 'OF']);
  });

  it('should show all flags as unset when all false', () => {
    const flags: CpuFlags = { zero: false, carry: false, sign: false, overflow: false };
    buildFlagEntries(flags).forEach(f => expect(f.set).toBe(false));
  });

  it('should reflect zero flag set', () => {
    const flags: CpuFlags = { zero: true, carry: false, sign: false, overflow: false };
    const zf = buildFlagEntries(flags).find(f => f.name === 'ZF')!;
    expect(zf.set).toBe(true);
  });

  it('should reflect carry flag set', () => {
    const flags: CpuFlags = { zero: false, carry: true, sign: false, overflow: false };
    const cf = buildFlagEntries(flags).find(f => f.name === 'CF')!;
    expect(cf.set).toBe(true);
  });

  it('should reflect sign flag set', () => {
    const flags: CpuFlags = { zero: false, carry: false, sign: true, overflow: false };
    const sf = buildFlagEntries(flags).find(f => f.name === 'SF')!;
    expect(sf.set).toBe(true);
  });

  it('should reflect overflow flag set', () => {
    const flags: CpuFlags = { zero: false, carry: false, sign: false, overflow: true };
    const of_ = buildFlagEntries(flags).find(f => f.name === 'OF')!;
    expect(of_.set).toBe(true);
  });

  it('should reflect multiple flags set', () => {
    const flags: CpuFlags = { zero: true, carry: true, sign: true, overflow: true };
    buildFlagEntries(flags).forEach(f => expect(f.set).toBe(true));
  });

  it('should keep unset flags false when others are set', () => {
    const flags: CpuFlags = { zero: true, carry: false, sign: true, overflow: false };
    const entries = buildFlagEntries(flags);
    expect(entries.find(f => f.name === 'ZF')!.set).toBe(true);
    expect(entries.find(f => f.name === 'CF')!.set).toBe(false);
    expect(entries.find(f => f.name === 'SF')!.set).toBe(true);
    expect(entries.find(f => f.name === 'OF')!.set).toBe(false);
  });
});
