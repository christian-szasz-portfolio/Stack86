import { describe, it, expect } from 'vitest';
import { ModrmUtil } from './modrm.util';

describe('ModrmUtil', () => {
  describe('encode / decode round-trip', () => {
    it('should encode and decode mod=11 reg=0 rm=3', () => {
      const byte = ModrmUtil.encode(0x03, 0, 3);
      const decoded = ModrmUtil.decode(byte);
      expect(decoded).toEqual({ mod: 3, reg: 0, rm: 3 });
    });

    it('should encode and decode mod=00 reg=5 rm=6', () => {
      const byte = ModrmUtil.encode(0x00, 5, 6);
      const decoded = ModrmUtil.decode(byte);
      expect(decoded).toEqual({ mod: 0, reg: 5, rm: 6 });
    });

    it('should encode and decode all field combinations', () => {
      for (let mod = 0; mod <= 3; mod++) {
        for (let reg = 0; reg <= 7; reg++) {
          for (let rm = 0; rm <= 7; rm++) {
            const byte = ModrmUtil.encode(mod, reg, rm);
            const decoded = ModrmUtil.decode(byte);
            expect(decoded).toEqual({ mod, reg, rm });
          }
        }
      }
    });
  });

  describe('register code lookups', () => {
    it('should return correct codes for 16-bit registers', () => {
      expect(ModrmUtil.wordRegCode('ax')).toBe(0);
      expect(ModrmUtil.wordRegCode('cx')).toBe(1);
      expect(ModrmUtil.wordRegCode('dx')).toBe(2);
      expect(ModrmUtil.wordRegCode('bx')).toBe(3);
      expect(ModrmUtil.wordRegCode('sp')).toBe(4);
      expect(ModrmUtil.wordRegCode('bp')).toBe(5);
      expect(ModrmUtil.wordRegCode('si')).toBe(6);
      expect(ModrmUtil.wordRegCode('di')).toBe(7);
    });

    it('should return correct codes for 8-bit registers', () => {
      expect(ModrmUtil.byteRegCode('al')).toBe(0);
      expect(ModrmUtil.byteRegCode('cl')).toBe(1);
      expect(ModrmUtil.byteRegCode('dl')).toBe(2);
      expect(ModrmUtil.byteRegCode('bl')).toBe(3);
      expect(ModrmUtil.byteRegCode('ah')).toBe(4);
      expect(ModrmUtil.byteRegCode('ch')).toBe(5);
      expect(ModrmUtil.byteRegCode('dh')).toBe(6);
      expect(ModrmUtil.byteRegCode('bh')).toBe(7);
    });

    it('should return -1 for unknown registers', () => {
      expect(ModrmUtil.wordRegCode('zz')).toBe(-1);
      expect(ModrmUtil.byteRegCode('zz')).toBe(-1);
      expect(ModrmUtil.registerCode('zz')).toBe(-1);
    });

    it('should handle case-insensitive lookups', () => {
      expect(ModrmUtil.wordRegCode('AX')).toBe(0);
      expect(ModrmUtil.byteRegCode('AL')).toBe(0);
      expect(ModrmUtil.registerCode('BX')).toBe(3);
    });
  });

  describe('segment register codes', () => {
    it('should return correct segment register codes', () => {
      expect(ModrmUtil.segRegCode('es')).toBe(0);
      expect(ModrmUtil.segRegCode('cs')).toBe(1);
      expect(ModrmUtil.segRegCode('ss')).toBe(2);
      expect(ModrmUtil.segRegCode('ds')).toBe(3);
    });
  });

  describe('isByteRegister / isWordRegister', () => {
    it('should identify byte registers', () => {
      expect(ModrmUtil.isByteRegister('al')).toBe(true);
      expect(ModrmUtil.isByteRegister('ah')).toBe(true);
      expect(ModrmUtil.isByteRegister('ax')).toBe(false);
    });

    it('should identify word registers', () => {
      expect(ModrmUtil.isWordRegister('ax')).toBe(true);
      expect(ModrmUtil.isWordRegister('sp')).toBe(true);
      expect(ModrmUtil.isWordRegister('al')).toBe(false);
    });
  });

  describe('encodeRegister', () => {
    it('should encode register-to-register (MOV AX, BX → mod=11)', () => {
      const bytes = ModrmUtil.encodeRegister(0, 'bx', false);
      expect(bytes).toHaveLength(1);
      const { mod, reg, rm } = ModrmUtil.decode(bytes[0]);
      expect(mod).toBe(3);
      expect(reg).toBe(0);
      expect(rm).toBe(3); // BX = 3
    });

    it('should encode byte register (AL, AH)', () => {
      const bytes = ModrmUtil.encodeRegister(2, 'al', true);
      const { mod, reg, rm } = ModrmUtil.decode(bytes[0]);
      expect(mod).toBe(3);
      expect(reg).toBe(2);
      expect(rm).toBe(0); // AL = 0
    });
  });

  describe('encodeMemory', () => {
    it('should encode direct addressing [nnnn]', () => {
      const bytes = ModrmUtil.encodeMemory(0, '', 0x1234);
      expect(bytes).toHaveLength(3);
      const { mod, rm } = ModrmUtil.decode(bytes[0]);
      expect(mod).toBe(0);
      expect(rm).toBe(6); // direct addressing
      expect(bytes[1]).toBe(0x34); // low byte
      expect(bytes[2]).toBe(0x12); // high byte
    });

    it('should encode [BX] with no displacement', () => {
      const bytes = ModrmUtil.encodeMemory(0, 'bx', 0);
      expect(bytes).toHaveLength(1);
      const { mod, rm } = ModrmUtil.decode(bytes[0]);
      expect(mod).toBe(0);
      expect(rm).toBe(7); // BX = 7
    });

    it('should encode [BP] with zero displacement using mod=01', () => {
      const bytes = ModrmUtil.encodeMemory(0, 'bp', 0);
      expect(bytes).toHaveLength(2);
      const { mod, rm } = ModrmUtil.decode(bytes[0]);
      expect(mod).toBe(1); // [BP] needs mod=01 to avoid direct addressing
      expect(rm).toBe(6);
      expect(bytes[1]).toBe(0); // disp8 = 0
    });

    it('should encode [BP-2] with 8-bit displacement', () => {
      const bytes = ModrmUtil.encodeMemory(3, 'bp', -2);
      expect(bytes).toHaveLength(2);
      const { mod, rm } = ModrmUtil.decode(bytes[0]);
      expect(mod).toBe(1);
      expect(rm).toBe(6);
      expect(bytes[1]).toBe(0xFE); // -2 as unsigned byte
    });

    it('should encode [BX+SI] with 16-bit displacement', () => {
      const bytes = ModrmUtil.encodeMemory(0, 'bx+si', 0x1234);
      expect(bytes).toHaveLength(3);
      const { mod, rm } = ModrmUtil.decode(bytes[0]);
      expect(mod).toBe(2); // 16-bit displacement
      expect(rm).toBe(0); // BX+SI = 0
    });
  });

  describe('decodeMemory', () => {
    it('should decode direct addressing', () => {
      const data = new Uint8Array([0x34, 0x12]);
      const result = ModrmUtil.decodeMemory(0, 6, data, 0);
      expect(result.base).toBe('');
      expect(result.displacement).toBe(0x1234);
      expect(result.bytesConsumed).toBe(2);
    });

    it('should decode [BX] with no displacement', () => {
      const data = new Uint8Array([]);
      const result = ModrmUtil.decodeMemory(0, 7, data, 0);
      expect(result.base).toBe('BX');
      expect(result.displacement).toBe(0);
      expect(result.bytesConsumed).toBe(0);
    });

    it('should decode [BP+disp8]', () => {
      const data = new Uint8Array([0xFE]);
      const result = ModrmUtil.decodeMemory(1, 6, data, 0);
      expect(result.base).toBe('BP');
      expect(result.displacement).toBe(-2);
      expect(result.bytesConsumed).toBe(1);
    });

    it('should decode [BX+SI+disp16]', () => {
      const data = new Uint8Array([0x34, 0x12]);
      const result = ModrmUtil.decodeMemory(2, 0, data, 0);
      expect(result.base).toBe('BX+SI');
      expect(result.displacement).toBe(0x1234);
      expect(result.bytesConsumed).toBe(2);
    });
  });

  describe('reverse lookups', () => {
    it('should return register names from codes', () => {
      expect(ModrmUtil.wordRegName(0)).toBe('AX');
      expect(ModrmUtil.wordRegName(3)).toBe('BX');
      expect(ModrmUtil.byteRegName(0)).toBe('AL');
      expect(ModrmUtil.byteRegName(4)).toBe('AH');
      expect(ModrmUtil.segRegName(3)).toBe('DS');
    });
  });
});
