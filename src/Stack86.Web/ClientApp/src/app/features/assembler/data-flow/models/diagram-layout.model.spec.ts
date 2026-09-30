import { describe, expect, it } from 'vitest';
import {
  ALL_REGISTERS,
  BusType,
  COLORS,
  REGISTER_ID_MAP,
  REG_AX,
  REG_IP,
  VIRTUAL_HEIGHT,
  VIRTUAL_WIDTH,
} from './diagram-layout.model';

describe('diagram-layout.model', () => {
  it('VIRTUAL dimensions are positive', () => {
    expect(VIRTUAL_WIDTH).toBeGreaterThan(0);
    expect(VIRTUAL_HEIGHT).toBeGreaterThan(0);
  });

  it('BusType enum has all expected variants', () => {
    expect(BusType.Data).toBe('data');
    expect(BusType.Address).toBe('address');
    expect(BusType.Control).toBe('control');
    expect(BusType.Internal).toBe('internal');
  });

  it('ALL_REGISTERS contains both general and segment registers', () => {
    const ids = ALL_REGISTERS.map((r) => r.id);
    expect(ids).toEqual(
      expect.arrayContaining(['ax', 'bx', 'cx', 'dx', 'sp', 'bp', 'si', 'di', 'cs', 'ds', 'es', 'ss', 'ip']),
    );
  });

  it('REGISTER_ID_MAP maps sub-registers (AH/AL/BH/BL/...) back to their parent', () => {
    expect(REGISTER_ID_MAP['ah']).toBe(REG_AX);
    expect(REGISTER_ID_MAP['al']).toBe(REG_AX);
    expect(REGISTER_ID_MAP['ip']).toBe(REG_IP);
  });

  it('every register has positive dimensions', () => {
    for (const reg of ALL_REGISTERS) {
      expect(reg.width).toBeGreaterThan(0);
      expect(reg.height).toBeGreaterThan(0);
    }
  });

  it('COLORS palette has bus colors for every bus type', () => {
    expect(COLORS.busData).toMatch(/^#/);
    expect(COLORS.busAddress).toMatch(/^#/);
    expect(COLORS.busControl).toMatch(/^#/);
    expect(COLORS.busInternal).toMatch(/^#/);
  });
});
