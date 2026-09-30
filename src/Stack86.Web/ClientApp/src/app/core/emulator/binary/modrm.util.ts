/**
 * ModR/M byte encoding/decoding for 8086 instructions.
 *
 * ModR/M layout: [mod(2 bits)][reg(3 bits)][rm(3 bits)]
 *
 * mod=00: memory, no displacement (except rm=110 → direct address)
 * mod=01: memory + 8-bit displacement
 * mod=10: memory + 16-bit displacement
 * mod=11: register-to-register
 */

/** 16-bit register → 3-bit code mapping (used in both reg and r/m fields). */
const WORD_REG_CODE: Readonly<Record<string, number>> = {
  ax: 0, cx: 1, dx: 2, bx: 3,
  sp: 4, bp: 5, si: 6, di: 7,
};

/** 8-bit register → 3-bit code mapping. */
const BYTE_REG_CODE: Readonly<Record<string, number>> = {
  al: 0, cl: 1, dl: 2, bl: 3,
  ah: 4, ch: 5, dh: 6, bh: 7,
};

/** Segment register → 2-bit code mapping (used in PUSH/POP seg encoding). */
const SEG_REG_CODE: Readonly<Record<string, number>> = {
  es: 0, cs: 1, ss: 2, ds: 3,
};

/** r/m field values for 16-bit memory addressing modes (mod != 11). */
const MEM_RM_CODE: Readonly<Record<string, number>> = {
  'bx+si': 0, 'bx+di': 1, 'bp+si': 2, 'bp+di': 3,
  si: 4, di: 5, bp: 6, bx: 7,
};

export class ModrmUtil {
  /** Build a ModR/M byte from its three fields. */
  public static encode(mod: number, reg: number, rm: number): number {
    return ((mod & 0x03) << 6) | ((reg & 0x07) << 3) | (rm & 0x07);
  }

  /** Decode a ModR/M byte into its three fields. */
  public static decode(byte: number): { mod: number; reg: number; rm: number } {
    return {
      mod: (byte >> 6) & 0x03,
      reg: (byte >> 3) & 0x07,
      rm: byte & 0x07,
    };
  }

  /** Look up the 3-bit code for a 16-bit register name. Returns -1 if not found. */
  public static wordRegCode(name: string): number {
    return WORD_REG_CODE[name.toLowerCase()] ?? -1;
  }

  /** Look up the 3-bit code for an 8-bit register name. Returns -1 if not found. */
  public static byteRegCode(name: string): number {
    return BYTE_REG_CODE[name.toLowerCase()] ?? -1;
  }

  /** Look up the 3-bit code for any (byte or word) register by name. Returns -1 if not found. */
  public static registerCode(name: string): number {
    const lower = name.toLowerCase();
    return WORD_REG_CODE[lower] ?? BYTE_REG_CODE[lower] ?? -1;
  }

  /** Look up the 2-bit code for a segment register. Returns -1 if not found. */
  public static segRegCode(name: string): number {
    return SEG_REG_CODE[name.toLowerCase()] ?? -1;
  }

  /** Check whether a register name is an 8-bit register. */
  public static isByteRegister(name: string): boolean {
    return name.toLowerCase() in BYTE_REG_CODE;
  }

  /** Check whether a register name is a 16-bit register. */
  public static isWordRegister(name: string): boolean {
    return name.toLowerCase() in WORD_REG_CODE;
  }

  /**
   * Build ModR/M byte + displacement bytes for a memory operand.
   *
   * @param regField - The 3-bit reg/opcode field value.
   * @param baseReg  - Base register name from the memory expression (e.g. 'bx', 'bp+si'), or empty for direct addressing.
   * @param displacement - Numeric displacement (e.g. -2 for [BP-2]).
   * @returns The ModR/M byte followed by any displacement bytes.
   */
  public static encodeMemory(regField: number, baseReg: string, displacement: number): number[] {
    const lower = baseReg.toLowerCase();

    // Direct addressing: [nnnn]
    if (lower === '' || lower === 'direct') {
      const modrm = ModrmUtil.encode(0x00, regField, 0x06);
      return [modrm, displacement & 0xFF, (displacement >> 8) & 0xFF];
    }

    const rmCode = MEM_RM_CODE[lower];
    if (rmCode === undefined) {
      throw new Error(`Unsupported memory addressing mode: [${baseReg}]`);
    }

    // [BP] with no displacement is a special case — must use mod=01 with disp8=0
    // because mod=00 rm=110 means direct addressing, not [BP].
    if (displacement === 0 && rmCode !== 6) {
      return [ModrmUtil.encode(0x00, regField, rmCode)];
    }

    if (displacement >= -128 && displacement <= 127) {
      return [
        ModrmUtil.encode(0x01, regField, rmCode),
        displacement & 0xFF,
      ];
    }

    return [
      ModrmUtil.encode(0x02, regField, rmCode),
      displacement & 0xFF,
      (displacement >> 8) & 0xFF,
    ];
  }

  /**
   * Build ModR/M byte for a register-to-register operation.
   *
   * @param regField - The 3-bit reg field value (source or opcode extension).
   * @param rmReg    - The register in the r/m field.
   * @param isByte   - Whether this is an 8-bit operation.
   * @returns Single-element array with the ModR/M byte.
   */
  public static encodeRegister(regField: number, rmReg: string, isByte: boolean): number[] {
    const code = isByte ? ModrmUtil.byteRegCode(rmReg) : ModrmUtil.wordRegCode(rmReg);
    if (code === -1) {
      throw new Error(`Unknown register for ModR/M: ${rmReg}`);
    }
    return [ModrmUtil.encode(0x03, regField, code)];
  }

  /**
   * Decode a memory operand from a byte stream.
   *
   * @param mod  - The mod field (0, 1, or 2).
   * @param rm   - The r/m field.
   * @param bytes - Remaining byte stream (positioned after the ModR/M byte).
   * @param offset - Current offset into bytes.
   * @returns Decoded base register name, displacement value, and bytes consumed.
   */
  public static decodeMemory(
    mod: number,
    rm: number,
    bytes: Uint8Array,
    offset: number,
  ): { base: string; displacement: number; bytesConsumed: number } {
    // Direct addressing
    if (mod === 0 && rm === 6) {
      const disp = bytes[offset] | (bytes[offset + 1] << 8);
      return { base: '', displacement: disp, bytesConsumed: 2 };
    }

    const RM_TO_BASE: readonly string[] = [
      'BX+SI', 'BX+DI', 'BP+SI', 'BP+DI', 'SI', 'DI', 'BP', 'BX',
    ];
    const base = RM_TO_BASE[rm];

    if (mod === 0) {
      return { base, displacement: 0, bytesConsumed: 0 };
    }

    if (mod === 1) {
      const disp8 = bytes[offset];
      const signed = disp8 >= 0x80 ? disp8 - 0x100 : disp8;
      return { base, displacement: signed, bytesConsumed: 1 };
    }

    // mod === 2
    const disp16 = bytes[offset] | (bytes[offset + 1] << 8);
    const signed16 = disp16 >= 0x8000 ? disp16 - 0x10000 : disp16;
    return { base, displacement: signed16, bytesConsumed: 2 };
  }

  /** Reverse lookup: 3-bit code → 16-bit register name. */
  public static wordRegName(code: number): string {
    const names = ['AX', 'CX', 'DX', 'BX', 'SP', 'BP', 'SI', 'DI'];
    return names[code & 0x07];
  }

  /** Reverse lookup: 3-bit code → 8-bit register name. */
  public static byteRegName(code: number): string {
    const names = ['AL', 'CL', 'DL', 'BL', 'AH', 'CH', 'DH', 'BH'];
    return names[code & 0x07];
  }

  /** Reverse lookup: 2-bit code → segment register name. */
  public static segRegName(code: number): string {
    const names = ['ES', 'CS', 'SS', 'DS'];
    return names[code & 0x03];
  }
}
