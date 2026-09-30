/**
 * 8086 CPU state — pure data, framework-agnostic.
 */

export interface CpuFlags {
  zero: boolean;
  carry: boolean;
  sign: boolean;
  overflow: boolean;
}

export interface CpuState {
  /** General-purpose registers (16-bit) */
  ax: number;
  bx: number;
  cx: number;
  dx: number;

  /** Index / pointer registers */
  sp: number;
  bp: number;
  si: number;
  di: number;

  /** Instruction pointer */
  ip: number;

  /** Segment registers */
  cs: number;
  ds: number;
  es: number;
  ss: number;

  /** Status flags */
  flags: CpuFlags;
}

export enum RegisterName {
  AX = 'ax',
  BX = 'bx',
  CX = 'cx',
  DX = 'dx',
  SP = 'sp',
  BP = 'bp',
  SI = 'si',
  DI = 'di',
  IP = 'ip',
}

export enum SegmentRegisterName {
  CS = 'cs',
  DS = 'ds',
  ES = 'es',
  SS = 'ss',
}

export enum ByteShift {
  High = 'high',
  Low = 'low',
}

export class CpuHelper {
  public static readonly REGISTER_NAMES: readonly RegisterName[] = [
    RegisterName.AX, RegisterName.BX, RegisterName.CX, RegisterName.DX,
    RegisterName.SP, RegisterName.BP, RegisterName.SI, RegisterName.DI, RegisterName.IP,
  ];

  public static readonly SUB_REGISTER_MAP: Readonly<Record<string, { parent: RegisterName; shift: ByteShift }>> = {
    ah: { parent: RegisterName.AX, shift: ByteShift.High },
    al: { parent: RegisterName.AX, shift: ByteShift.Low },
    bh: { parent: RegisterName.BX, shift: ByteShift.High },
    bl: { parent: RegisterName.BX, shift: ByteShift.Low },
    ch: { parent: RegisterName.CX, shift: ByteShift.High },
    cl: { parent: RegisterName.CX, shift: ByteShift.Low },
    dh: { parent: RegisterName.DX, shift: ByteShift.High },
    dl: { parent: RegisterName.DX, shift: ByteShift.Low },
  };

  public static readonly SEGMENT_REGISTER_NAMES: readonly SegmentRegisterName[] = [
    SegmentRegisterName.CS, SegmentRegisterName.DS, SegmentRegisterName.ES, SegmentRegisterName.SS,
  ];

  public static createInitialState(): CpuState {
    return {
      ax: 0, bx: 0, cx: 0, dx: 0,
      sp: 0xFFFE, bp: 0, si: 0, di: 0,
      ip: 0,
      cs: 0, ds: 0, es: 0, ss: 0,
      flags: { zero: false, carry: false, sign: false, overflow: false },
    };
  }

  public static getHighByte(value: number): number {
    return (value >> 8) & 0xFF;
  }

  public static getLowByte(value: number): number {
    return value & 0xFF;
  }

  public static setHighByte(reg: number, value: number): number {
    return (reg & 0x00FF) | ((value & 0xFF) << 8);
  }

  public static setLowByte(reg: number, value: number): number {
    return (reg & 0xFF00) | (value & 0xFF);
  }

  public static toWord(value: number): number {
    return value & 0xFFFF;
  }

  public static toByte(value: number): number {
    return value & 0xFF;
  }

  public static toSigned16(value: number): number {
    const v = value & 0xFFFF;
    return v >= 0x8000 ? v - 0x10000 : v;
  }

  public static toSigned8(value: number): number {
    const v = value & 0xFF;
    return v >= 0x80 ? v - 0x100 : v;
  }

  public static isRegister(name: string): name is RegisterName {
    return Object.values(RegisterName).includes(name.toLowerCase() as RegisterName);
  }

  public static isSubRegister(name: string): boolean {
    return name.toLowerCase() in CpuHelper.SUB_REGISTER_MAP;
  }

  public static isSegmentRegister(name: string): name is SegmentRegisterName {
    return Object.values(SegmentRegisterName).includes(name.toLowerCase() as SegmentRegisterName);
  }
}
