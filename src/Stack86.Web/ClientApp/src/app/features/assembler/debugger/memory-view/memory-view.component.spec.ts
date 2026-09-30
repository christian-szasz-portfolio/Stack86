import { describe, it, expect, beforeEach } from 'vitest';

/**
 * Tests the memory view display logic used by MemoryViewComponent.
 * Mirrors the computed signal transformation without Angular DI.
 */

interface MemoryRow {
  address: string;
  bytes: string[];
  ascii: string;
}

function buildMemoryRows(memory: number[], startAddress: number, rowCount = 16): MemoryRow[] {
  const rows: MemoryRow[] = [];

  for (let r = 0; r < rowCount; r++) {
    const addr = startAddress + r * 16;
    const bytes: string[] = [];
    let ascii = '';

    for (let c = 0; c < 16; c++) {
      const idx = addr + c;
      const val = idx < memory.length ? memory[idx] : 0;
      bytes.push(val.toString(16).toUpperCase().padStart(2, '0'));
      ascii += val >= 32 && val <= 126 ? String.fromCharCode(val) : '.';
    }

    rows.push({
      address: addr.toString(16).toUpperCase().padStart(4, '0'),
      bytes,
      ascii,
    });
  }

  return rows;
}

function parseAddress(input: string): number | null {
  const parsed = parseInt(input, 16);
  if (!isNaN(parsed) && parsed >= 0 && parsed < 0x10000) {
    return parsed & 0xFFF0;
  }
  return null;
}

describe('MemoryViewComponent logic', () => {
  let memory: number[];

  beforeEach(() => {
    memory = new Array(0x10000).fill(0);
  });

  it('should produce 16 rows by default', () => {
    const rows = buildMemoryRows(memory, 0);
    expect(rows).toHaveLength(16);
  });

  it('should format addresses as 4-digit hex', () => {
    const rows = buildMemoryRows(memory, 0);
    expect(rows[0].address).toBe('0000');
    expect(rows[1].address).toBe('0010');
    expect(rows[15].address).toBe('00F0');
  });

  it('should produce 16 byte columns per row', () => {
    const rows = buildMemoryRows(memory, 0);
    rows.forEach(row => expect(row.bytes).toHaveLength(16));
  });

  it('should format zero bytes as 00', () => {
    const rows = buildMemoryRows(memory, 0);
    rows[0].bytes.forEach(b => expect(b).toBe('00'));
  });

  it('should format non-zero bytes correctly', () => {
    memory[0] = 0xFF;
    memory[1] = 0x0A;
    memory[2] = 0x42;
    const rows = buildMemoryRows(memory, 0);
    expect(rows[0].bytes[0]).toBe('FF');
    expect(rows[0].bytes[1]).toBe('0A');
    expect(rows[0].bytes[2]).toBe('42');
  });

  it('should show dots for non-printable ASCII', () => {
    const rows = buildMemoryRows(memory, 0);
    expect(rows[0].ascii).toBe('................');
  });

  it('should show printable ASCII characters', () => {
    memory[0] = 65; // 'A'
    memory[1] = 66; // 'B'
    memory[2] = 48; // '0'
    const rows = buildMemoryRows(memory, 0);
    expect(rows[0].ascii.startsWith('AB0')).toBe(true);
  });

  it('should use start address for row offsets', () => {
    const rows = buildMemoryRows(memory, 0x100);
    expect(rows[0].address).toBe('0100');
    expect(rows[1].address).toBe('0110');
  });

  it('should show data at correct offsets', () => {
    memory[0x105] = 0xAB;
    const rows = buildMemoryRows(memory, 0x100);
    expect(rows[0].bytes[5]).toBe('AB');
  });

  describe('address parsing', () => {
    it('should parse valid hex address', () => {
      expect(parseAddress('0100')).toBe(0x0100);
    });

    it('should align to 16-byte boundary', () => {
      expect(parseAddress('0105')).toBe(0x0100);
      expect(parseAddress('001F')).toBe(0x0010);
    });

    it('should reject invalid input', () => {
      expect(parseAddress('ZZZZ')).toBeNull();
    });

    it('should reject negative-ish out of range', () => {
      expect(parseAddress('FFFFF')).toBeNull();
    });

    it('should handle zero', () => {
      expect(parseAddress('0000')).toBe(0);
    });

    it('should handle FFFF', () => {
      expect(parseAddress('FFFF')).toBe(0xFFF0);
    });
  });
});
