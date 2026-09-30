import { describe, it, expect, beforeEach } from 'vitest';
import { CpuState, CpuHelper } from '@core/emulator/cpu/cpu.model';

/**
 * Tests the register display logic used by RegistersComponent.
 * Mirrors the computed signal transformation without Angular DI.
 */

interface RegisterEntry {
  name: string;
  hex: string;
  dec: number;
  changed: boolean;
}

const REGISTER_KEYS: readonly (keyof Pick<CpuState, 'ax' | 'bx' | 'cx' | 'dx' | 'sp' | 'bp' | 'si' | 'di' | 'ip' | 'cs' | 'ds' | 'es' | 'ss'>)[] =
  ['ax', 'bx', 'cx', 'dx', 'sp', 'bp', 'si', 'di', 'ip', 'cs', 'ds', 'es', 'ss'];

function buildRegisterEntries(
  regs: CpuState,
  previous: Record<string, number>,
): RegisterEntry[] {
  return REGISTER_KEYS.map((name) => {
    const value = regs[name];
    const changed = previous[name] !== undefined && previous[name] !== value;
    return {
      name: name.toUpperCase(),
      hex: value.toString(16).toUpperCase().padStart(4, '0') + 'h',
      dec: value,
      changed,
    };
  });
}

describe('RegistersComponent logic', () => {
  let cpu: CpuState;

  beforeEach(() => {
    cpu = CpuHelper.createInitialState();
  });

  it('should produce 13 register entries', () => {
    const entries = buildRegisterEntries(cpu, {});
    expect(entries).toHaveLength(13);
  });

  it('should format register names in uppercase', () => {
    const names = buildRegisterEntries(cpu, {}).map(e => e.name);
    expect(names).toEqual(['AX', 'BX', 'CX', 'DX', 'SP', 'BP', 'SI', 'DI', 'IP', 'CS', 'DS', 'ES', 'SS']);
  });

  it('should format hex values with 4 digits and h suffix', () => {
    const entry = buildRegisterEntries(cpu, {}).find(e => e.name === 'AX')!;
    expect(entry.hex).toBe('0000h');
  });

  it('should show SP initial value as FFFEh', () => {
    const entry = buildRegisterEntries(cpu, {}).find(e => e.name === 'SP')!;
    expect(entry.hex).toBe('FFFEh');
    expect(entry.dec).toBe(0xFFFE);
  });

  it('should not mark entries as changed on first read', () => {
    const entries = buildRegisterEntries(cpu, {});
    entries.forEach(e => expect(e.changed).toBe(false));
  });

  it('should mark entries as changed when value differs from previous', () => {
    const previous: Record<string, number> = { ax: 0, bx: 0, cx: 0, dx: 0, sp: 0xFFFE, bp: 0, si: 0, di: 0, ip: 0 };
    cpu.ax = 42;
    const entries = buildRegisterEntries(cpu, previous);
    const ax = entries.find(e => e.name === 'AX')!;
    expect(ax.changed).toBe(true);
    expect(ax.dec).toBe(42);
    expect(ax.hex).toBe('002Ah');
  });

  it('should not mark unchanged registers as changed', () => {
    const previous: Record<string, number> = { ax: 0, bx: 0, cx: 0, dx: 0, sp: 0xFFFE, bp: 0, si: 0, di: 0, ip: 0 };
    cpu.ax = 42;
    const entries = buildRegisterEntries(cpu, previous);
    const bx = entries.find(e => e.name === 'BX')!;
    expect(bx.changed).toBe(false);
  });

  it('should format large values correctly', () => {
    cpu.ax = 0xFFFF;
    const entry = buildRegisterEntries(cpu, {}).find(e => e.name === 'AX')!;
    expect(entry.hex).toBe('FFFFh');
    expect(entry.dec).toBe(65535);
  });

  it('should format single-digit hex with leading zeros', () => {
    cpu.bx = 1;
    const entry = buildRegisterEntries(cpu, {}).find(e => e.name === 'BX')!;
    expect(entry.hex).toBe('0001h');
  });
});
