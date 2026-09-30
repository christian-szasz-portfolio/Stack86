import { describe, it, expect } from 'vitest';
import { Memory } from './memory.model';

describe('Memory', () => {
  it('should have a 64KB address space', () => {
    const mem = new Memory();
    expect(mem.size).toBe(65536);
  });

  it('should initialize all bytes to zero', () => {
    const mem = new Memory();
    expect(mem.readByte(0)).toBe(0);
    expect(mem.readByte(0x7FFF)).toBe(0);
    expect(mem.readByte(0xFFFF)).toBe(0);
  });

  describe('byte access', () => {
    it('should write and read a byte', () => {
      const mem = new Memory();
      mem.writeByte(0x100, 0xAB);
      expect(mem.readByte(0x100)).toBe(0xAB);
    });

    it('should mask values to 8 bits', () => {
      const mem = new Memory();
      mem.writeByte(0x00, 0x1FF);
      expect(mem.readByte(0x00)).toBe(0xFF);
    });

    it('should throw on out-of-bounds read', () => {
      const mem = new Memory();
      expect(() => mem.readByte(-1)).toThrow(RangeError);
      expect(() => mem.readByte(0x10000)).toThrow(RangeError);
    });

    it('should throw on out-of-bounds write', () => {
      const mem = new Memory();
      expect(() => mem.writeByte(-1, 0)).toThrow(RangeError);
      expect(() => mem.writeByte(0x10000, 0)).toThrow(RangeError);
    });
  });

  describe('word access (little-endian)', () => {
    it('should write and read a word in little-endian order', () => {
      const mem = new Memory();
      mem.writeWord(0x200, 0xBEEF);
      expect(mem.readByte(0x200)).toBe(0xEF); // low byte first
      expect(mem.readByte(0x201)).toBe(0xBE); // high byte second
      expect(mem.readWord(0x200)).toBe(0xBEEF);
    });

    it('should handle word at address 0', () => {
      const mem = new Memory();
      mem.writeWord(0, 0x1234);
      expect(mem.readWord(0)).toBe(0x1234);
    });

    it('should throw when word straddles end of memory', () => {
      const mem = new Memory();
      expect(() => mem.readWord(0xFFFF)).toThrow(RangeError);
      expect(() => mem.writeWord(0xFFFF, 0)).toThrow(RangeError);
    });
  });

  describe('loadProgram', () => {
    it('should load bytes at offset', () => {
      const mem = new Memory();
      mem.loadProgram([0x01, 0x02, 0x03], 0x100);
      expect(mem.readByte(0x100)).toBe(0x01);
      expect(mem.readByte(0x101)).toBe(0x02);
      expect(mem.readByte(0x102)).toBe(0x03);
    });

    it('should load at offset 0 by default', () => {
      const mem = new Memory();
      mem.loadProgram([0xAA, 0xBB]);
      expect(mem.readByte(0)).toBe(0xAA);
      expect(mem.readByte(1)).toBe(0xBB);
    });

    it('should throw if program exceeds address space', () => {
      const mem = new Memory();
      expect(() => mem.loadProgram([0], 0x10000)).toThrow(RangeError);
      expect(() => mem.loadProgram(new Array(10), 0xFFFA)).toThrow(RangeError);
    });
  });

  describe('slice', () => {
    it('should return a copy of a region', () => {
      const mem = new Memory();
      mem.writeByte(0x00, 0xAA);
      mem.writeByte(0x01, 0xBB);
      mem.writeByte(0x02, 0xCC);
      const slice = mem.slice(0x00, 3);
      expect(slice).toEqual(new Uint8Array([0xAA, 0xBB, 0xCC]));
    });

    it('should not be affected by subsequent writes (is a copy)', () => {
      const mem = new Memory();
      mem.writeByte(0, 0xFF);
      const slice = mem.slice(0, 1);
      mem.writeByte(0, 0x00);
      expect(slice[0]).toBe(0xFF);
    });
  });

  describe('reset', () => {
    it('should zero all memory', () => {
      const mem = new Memory();
      mem.writeByte(0x00, 0xFF);
      mem.writeByte(0xFFFF, 0xFF);
      mem.reset();
      expect(mem.readByte(0x00)).toBe(0);
      expect(mem.readByte(0xFFFF)).toBe(0);
    });
  });
});
