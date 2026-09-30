import { describe, it, expect } from 'vitest';

/**
 * Tests the stack view display logic used by StackViewComponent.
 * Mirrors the computed signal transformation without Angular DI.
 */

interface StackEntry {
  address: number;
  value: number;
}

interface StackDisplayEntry {
  address: string;
  value: string;
  isSp: boolean;
}

function buildStackEntries(stackView: StackEntry[], sp: number): StackDisplayEntry[] {
  return stackView.map((entry) => ({
    address: entry.address.toString(16).toUpperCase().padStart(4, '0'),
    value: entry.value.toString(16).toUpperCase().padStart(4, '0'),
    isSp: entry.address === sp,
  }));
}

describe('StackViewComponent logic', () => {
  it('should produce empty array for empty stack', () => {
    expect(buildStackEntries([], 0xFFFE)).toEqual([]);
  });

  it('should format address as 4-digit hex', () => {
    const entries = buildStackEntries([{ address: 0xFFFC, value: 0x1234 }], 0xFFFC);
    expect(entries[0].address).toBe('FFFC');
  });

  it('should format value as 4-digit hex', () => {
    const entries = buildStackEntries([{ address: 0xFFFC, value: 0x1234 }], 0xFFFC);
    expect(entries[0].value).toBe('1234');
  });

  it('should mark current SP entry', () => {
    const entries = buildStackEntries(
      [
        { address: 0xFFFC, value: 0x1234 },
        { address: 0xFFFE, value: 0x5678 },
      ],
      0xFFFC,
    );
    expect(entries[0].isSp).toBe(true);
    expect(entries[1].isSp).toBe(false);
  });

  it('should handle multiple entries', () => {
    const stack: StackEntry[] = [
      { address: 0xFFFA, value: 0x0001 },
      { address: 0xFFFC, value: 0x0002 },
      { address: 0xFFFE, value: 0x0003 },
    ];
    const entries = buildStackEntries(stack, 0xFFFA);
    expect(entries).toHaveLength(3);
    expect(entries[0].isSp).toBe(true);
    expect(entries[1].isSp).toBe(false);
    expect(entries[2].isSp).toBe(false);
  });

  it('should pad small values with leading zeros', () => {
    const entries = buildStackEntries([{ address: 0x0010, value: 0x0001 }], 0x0010);
    expect(entries[0].address).toBe('0010');
    expect(entries[0].value).toBe('0001');
  });

  it('should handle zero values', () => {
    const entries = buildStackEntries([{ address: 0xFFFE, value: 0 }], 0xFFFE);
    expect(entries[0].value).toBe('0000');
  });
});
