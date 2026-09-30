import { CpuState } from '../cpu/cpu.model';
import { CpuHelper } from '../cpu/cpu.model';

export class FlagHelper {
  public static updateArithmetic16(cpu: CpuState, result: number, carryOut: boolean): void {
    const masked = CpuHelper.toWord(result);
    cpu.flags.zero = masked === 0;
    cpu.flags.sign = (masked & 0x8000) !== 0;
    cpu.flags.carry = carryOut;
    cpu.flags.overflow = result > 0x7FFF || result < -0x8000;
  }

  public static updateArithmetic8(cpu: CpuState, result: number, carryOut: boolean): void {
    const masked = CpuHelper.toByte(result);
    cpu.flags.zero = masked === 0;
    cpu.flags.sign = (masked & 0x80) !== 0;
    cpu.flags.carry = carryOut;
    cpu.flags.overflow = result > 0x7F || result < -0x80;
  }

  public static updateLogic16(cpu: CpuState, result: number): void {
    const masked = CpuHelper.toWord(result);
    cpu.flags.zero = masked === 0;
    cpu.flags.sign = (masked & 0x8000) !== 0;
    cpu.flags.carry = false;
    cpu.flags.overflow = false;
  }

  public static updateLogic8(cpu: CpuState, result: number): void {
    const masked = CpuHelper.toByte(result);
    cpu.flags.zero = masked === 0;
    cpu.flags.sign = (masked & 0x80) !== 0;
    cpu.flags.carry = false;
    cpu.flags.overflow = false;
  }
}
