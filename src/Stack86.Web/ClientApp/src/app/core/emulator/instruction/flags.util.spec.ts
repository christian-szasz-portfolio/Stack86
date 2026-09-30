import { describe, it, expect } from 'vitest';
import { FlagHelper } from './flags.util';
import { CpuHelper } from '../cpu/cpu.model';

describe('FlagHelper.updateArithmetic16', () => {
  it('should set zero flag for zero result', () => {
    const cpu = CpuHelper.createInitialState();
    FlagHelper.updateArithmetic16(cpu, 0, false);
    expect(cpu.flags.zero).toBe(true);
    expect(cpu.flags.sign).toBe(false);
    expect(cpu.flags.carry).toBe(false);
  });

  it('should set sign flag for negative result', () => {
    const cpu = CpuHelper.createInitialState();
    FlagHelper.updateArithmetic16(cpu, 0x8000, false);
    expect(cpu.flags.sign).toBe(true);
    expect(cpu.flags.zero).toBe(false);
  });

  it('should set carry flag when specified', () => {
    const cpu = CpuHelper.createInitialState();
    FlagHelper.updateArithmetic16(cpu, 0x10000, true);
    expect(cpu.flags.carry).toBe(true);
  });

  it('should set overflow for signed overflow', () => {
    const cpu = CpuHelper.createInitialState();
    FlagHelper.updateArithmetic16(cpu, 0x8000, false);
    expect(cpu.flags.overflow).toBe(true);
  });

  it('should clear flags for normal positive result', () => {
    const cpu = CpuHelper.createInitialState();
    cpu.flags.zero = true;
    cpu.flags.sign = true;
    cpu.flags.carry = true;
    cpu.flags.overflow = true;
    FlagHelper.updateArithmetic16(cpu, 42, false);
    expect(cpu.flags.zero).toBe(false);
    expect(cpu.flags.sign).toBe(false);
    expect(cpu.flags.carry).toBe(false);
    expect(cpu.flags.overflow).toBe(false);
  });
});

describe('FlagHelper.updateArithmetic8', () => {
  it('should set zero flag for zero result', () => {
    const cpu = CpuHelper.createInitialState();
    FlagHelper.updateArithmetic8(cpu, 0, false);
    expect(cpu.flags.zero).toBe(true);
  });

  it('should set sign flag for bit 7 set', () => {
    const cpu = CpuHelper.createInitialState();
    FlagHelper.updateArithmetic8(cpu, 0x80, false);
    expect(cpu.flags.sign).toBe(true);
  });
});

describe('FlagHelper.updateLogic16', () => {
  it('should clear carry and overflow', () => {
    const cpu = CpuHelper.createInitialState();
    cpu.flags.carry = true;
    cpu.flags.overflow = true;
    FlagHelper.updateLogic16(cpu, 0xFF);
    expect(cpu.flags.carry).toBe(false);
    expect(cpu.flags.overflow).toBe(false);
    expect(cpu.flags.zero).toBe(false);
    expect(cpu.flags.sign).toBe(false);
  });
});
