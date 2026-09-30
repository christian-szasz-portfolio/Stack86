import { describe, it, expect } from 'vitest';
import { CpuHelper, RegisterName, ByteShift } from './cpu.model';

describe('CpuHelper', () => {
  it('should create initial state with zeroed registers', () => {
    const cpu = CpuHelper.createInitialState();
    expect(cpu.ax).toBe(0);
    expect(cpu.bx).toBe(0);
    expect(cpu.cx).toBe(0);
    expect(cpu.dx).toBe(0);
    expect(cpu.bp).toBe(0);
    expect(cpu.si).toBe(0);
    expect(cpu.di).toBe(0);
    expect(cpu.ip).toBe(0);
  });

  it('should initialize SP to 0xFFFE', () => {
    const cpu = CpuHelper.createInitialState();
    expect(cpu.sp).toBe(0xFFFE);
  });

  it('should initialize all flags to false', () => {
    const cpu = CpuHelper.createInitialState();
    expect(cpu.flags.zero).toBe(false);
    expect(cpu.flags.carry).toBe(false);
    expect(cpu.flags.sign).toBe(false);
    expect(cpu.flags.overflow).toBe(false);
  });
});

describe('CpuHelper byte accessors', () => {
  it('getHighByte should return upper 8 bits', () => {
    expect(CpuHelper.getHighByte(0xABCD)).toBe(0xAB);
    expect(CpuHelper.getHighByte(0x00FF)).toBe(0x00);
    expect(CpuHelper.getHighByte(0xFF00)).toBe(0xFF);
  });

  it('getLowByte should return lower 8 bits', () => {
    expect(CpuHelper.getLowByte(0xABCD)).toBe(0xCD);
    expect(CpuHelper.getLowByte(0x00FF)).toBe(0xFF);
    expect(CpuHelper.getLowByte(0xFF00)).toBe(0x00);
  });

  it('setHighByte should replace upper 8 bits', () => {
    expect(CpuHelper.setHighByte(0x00CD, 0xAB)).toBe(0xABCD);
    expect(CpuHelper.setHighByte(0xFFFF, 0x00)).toBe(0x00FF);
  });

  it('setLowByte should replace lower 8 bits', () => {
    expect(CpuHelper.setLowByte(0xAB00, 0xCD)).toBe(0xABCD);
    expect(CpuHelper.setLowByte(0xFFFF, 0x00)).toBe(0xFF00);
  });

  it('round-trips high byte correctly', () => {
    const original = 0x1234;
    const modified = CpuHelper.setHighByte(original, 0xFF);
    expect(CpuHelper.getHighByte(modified)).toBe(0xFF);
    expect(CpuHelper.getLowByte(modified)).toBe(0x34);
  });

  it('round-trips low byte correctly', () => {
    const original = 0x1234;
    const modified = CpuHelper.setLowByte(original, 0xFF);
    expect(CpuHelper.getHighByte(modified)).toBe(0x12);
    expect(CpuHelper.getLowByte(modified)).toBe(0xFF);
  });
});

describe('CpuHelper toWord / toByte', () => {
  it('toWord clamps to 16-bit unsigned', () => {
    expect(CpuHelper.toWord(0x10000)).toBe(0);
    expect(CpuHelper.toWord(0x1FFFF)).toBe(0xFFFF);
    expect(CpuHelper.toWord(-1)).toBe(0xFFFF);
    expect(CpuHelper.toWord(255)).toBe(255);
  });

  it('toByte clamps to 8-bit unsigned', () => {
    expect(CpuHelper.toByte(0x100)).toBe(0);
    expect(CpuHelper.toByte(0x1FF)).toBe(0xFF);
    expect(CpuHelper.toByte(-1)).toBe(0xFF);
    expect(CpuHelper.toByte(127)).toBe(127);
  });
});

describe('CpuHelper register name helpers', () => {
  it('isRegister recognizes 16-bit registers', () => {
    expect(CpuHelper.isRegister('ax')).toBe(true);
    expect(CpuHelper.isRegister('AX')).toBe(true);
    expect(CpuHelper.isRegister('sp')).toBe(true);
    expect(CpuHelper.isRegister('ip')).toBe(true);
    expect(CpuHelper.isRegister('ah')).toBe(false);
    expect(CpuHelper.isRegister('foo')).toBe(false);
  });

  it('isSubRegister recognizes 8-bit sub-registers', () => {
    expect(CpuHelper.isSubRegister('ah')).toBe(true);
    expect(CpuHelper.isSubRegister('AL')).toBe(true);
    expect(CpuHelper.isSubRegister('dl')).toBe(true);
    expect(CpuHelper.isSubRegister('ax')).toBe(false);
    expect(CpuHelper.isSubRegister('sp')).toBe(false);
  });

  it('SUB_REGISTER_MAP has correct parent and shift', () => {
    expect(CpuHelper.SUB_REGISTER_MAP['ah']).toEqual({ parent: RegisterName.AX, shift: ByteShift.High });
    expect(CpuHelper.SUB_REGISTER_MAP['al']).toEqual({ parent: RegisterName.AX, shift: ByteShift.Low });
    expect(CpuHelper.SUB_REGISTER_MAP['dh']).toEqual({ parent: RegisterName.DX, shift: ByteShift.High });
    expect(CpuHelper.SUB_REGISTER_MAP['dl']).toEqual({ parent: RegisterName.DX, shift: ByteShift.Low });
  });
});
