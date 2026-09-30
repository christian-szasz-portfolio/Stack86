/**
 * X86 machine-code decoder (disassembler) for the Stack86 instruction subset.
 *
 * Takes a flat binary (COM file bytes) and produces formatted 8086 assembly source
 * that can be loaded into the editor and reassembled.
 *
 * Uses linear sweep disassembly. Jump/call targets generate labels.
 * Unrecognised opcodes are emitted as `DB` byte directives.
 */

import { ModrmUtil } from './modrm.util';

/** Decoded instruction before final text formatting. */
interface DecodedEntry {
  /** Offset in the binary (relative to start, not ORG). */
  readonly offset: number;
  /** Number of bytes consumed. */
  readonly size: number;
  /** Assembly mnemonic. */
  readonly mnemonic: string;
  /** Formatted operand string. */
  readonly operands: string;
  /** If this is a branch, the target offset (for label generation). */
  readonly branchTarget?: number;
}

export class X86Decoder {
  /**
   * Disassemble a COM binary into assembly source text.
   *
   * @param bytes - Raw COM file bytes.
   * @returns Assembly source string with `.MODEL SMALL` header.
   */
  public decode(bytes: Uint8Array): string {
    // Pass 1: decode all instructions and collect branch targets
    const entries = this.decodeAll(bytes);
    const branchTargets = new Set<number>();
    for (const entry of entries) {
      if (entry.branchTarget !== undefined && entry.branchTarget >= 0 && entry.branchTarget < bytes.length) {
        branchTargets.add(entry.branchTarget);
      }
    }

    // Detect data regions: strings terminated with '$' (DOS string convention)
    const dataRegions = X86Decoder.detectStringData(bytes);

    // Rebuild entry list incorporating data regions
    const finalEntries = this.mergeDataRegions(entries, dataRegions, bytes);

    // Pass 2: format source with labels
    return X86Decoder.formatSource(finalEntries, branchTargets);
  }

  /**
   * Linear sweep decode of all bytes.
   */
  private decodeAll(bytes: Uint8Array): DecodedEntry[] {
    const entries: DecodedEntry[] = [];
    let offset = 0;

    while (offset < bytes.length) {
      const entry = this.decodeAt(bytes, offset);
      entries.push(entry);
      offset += entry.size;
    }

    return entries;
  }

  /**
   * Decode a single instruction at the given offset.
   */
  private decodeAt(bytes: Uint8Array, offset: number): DecodedEntry {
    const b0 = bytes[offset];

    // ---------- Simple single-byte instructions ----------
    if (b0 === 0x90) return { offset, size: 1, mnemonic: 'NOP', operands: '' };
    if (b0 === 0xF4) return { offset, size: 1, mnemonic: 'HLT', operands: '' };
    if (b0 === 0xC3) return { offset, size: 1, mnemonic: 'RET', operands: '' };

    // ---------- INC r16 (40-47) / DEC r16 (48-4F) ----------
    if (b0 >= 0x40 && b0 <= 0x47) {
      return { offset, size: 1, mnemonic: 'INC', operands: ModrmUtil.wordRegName(b0 - 0x40) };
    }
    if (b0 >= 0x48 && b0 <= 0x4F) {
      return { offset, size: 1, mnemonic: 'DEC', operands: ModrmUtil.wordRegName(b0 - 0x48) };
    }

    // ---------- PUSH r16 (50-57) / POP r16 (58-5F) ----------
    if (b0 >= 0x50 && b0 <= 0x57) {
      return { offset, size: 1, mnemonic: 'PUSH', operands: ModrmUtil.wordRegName(b0 - 0x50) };
    }
    if (b0 >= 0x58 && b0 <= 0x5F) {
      return { offset, size: 1, mnemonic: 'POP', operands: ModrmUtil.wordRegName(b0 - 0x58) };
    }

    // ---------- PUSH seg / POP seg ----------
    if (b0 === 0x06 || b0 === 0x0E || b0 === 0x16 || b0 === 0x1E) {
      return { offset, size: 1, mnemonic: 'PUSH', operands: ModrmUtil.segRegName((b0 >> 3) & 0x03) };
    }
    if (b0 === 0x07 || b0 === 0x17 || b0 === 0x1F) {
      return { offset, size: 1, mnemonic: 'POP', operands: ModrmUtil.segRegName((b0 >> 3) & 0x03) };
    }

    // ---------- XCHG AX, r16 (91-97) ----------
    if (b0 >= 0x91 && b0 <= 0x97) {
      return { offset, size: 1, mnemonic: 'XCHG', operands: `AX, ${ModrmUtil.wordRegName(b0 - 0x90)}` };
    }

    // ---------- MOV r8, imm8 (B0-B7) ----------
    if (b0 >= 0xB0 && b0 <= 0xB7) {
      const reg = ModrmUtil.byteRegName(b0 - 0xB0);
      const imm = bytes[offset + 1];
      return { offset, size: 2, mnemonic: 'MOV', operands: `${reg}, ${X86Decoder.formatHex8(imm)}` };
    }

    // ---------- MOV r16, imm16 (B8-BF) ----------
    if (b0 >= 0xB8 && b0 <= 0xBF) {
      const reg = ModrmUtil.wordRegName(b0 - 0xB8);
      const imm = bytes[offset + 1] | (bytes[offset + 2] << 8);
      return { offset, size: 3, mnemonic: 'MOV', operands: `${reg}, ${X86Decoder.formatHex16(imm)}` };
    }

    // ---------- INT imm8 (CD) ----------
    if (b0 === 0xCD) {
      const imm = bytes[offset + 1];
      return { offset, size: 2, mnemonic: 'INT', operands: X86Decoder.formatHex8(imm) };
    }

    // ---------- Short JMP rel8 (EB) ----------
    if (b0 === 0xEB) {
      const rel = X86Decoder.signExtend8(bytes[offset + 1]);
      const target = offset + 2 + rel;
      return { offset, size: 2, mnemonic: 'JMP', operands: X86Decoder.labelName(target), branchTarget: target };
    }

    // ---------- Near JMP rel16 (E9) ----------
    if (b0 === 0xE9) {
      const rel = X86Decoder.signExtend16(bytes[offset + 1] | (bytes[offset + 2] << 8));
      const target = offset + 3 + rel;
      return { offset, size: 3, mnemonic: 'JMP', operands: X86Decoder.labelName(target), branchTarget: target };
    }

    // ---------- CALL rel16 (E8) ----------
    if (b0 === 0xE8) {
      const rel = X86Decoder.signExtend16(bytes[offset + 1] | (bytes[offset + 2] << 8));
      const target = offset + 3 + rel;
      return { offset, size: 3, mnemonic: 'CALL', operands: X86Decoder.labelName(target), branchTarget: target };
    }

    // ---------- Conditional jumps (70-7F) ----------
    if (b0 >= 0x70 && b0 <= 0x7F) {
      const rel = X86Decoder.signExtend8(bytes[offset + 1]);
      const target = offset + 2 + rel;
      const mnemonic = X86Decoder.condJumpMnemonic(b0);
      return { offset, size: 2, mnemonic, operands: X86Decoder.labelName(target), branchTarget: target };
    }

    // ---------- LOOP rel8 (E2) ----------
    if (b0 === 0xE2) {
      const rel = X86Decoder.signExtend8(bytes[offset + 1]);
      const target = offset + 2 + rel;
      return { offset, size: 2, mnemonic: 'LOOP', operands: X86Decoder.labelName(target), branchTarget: target };
    }

    // ---------- PUSH imm8 (6A) / PUSH imm16 (68) ----------
    if (b0 === 0x6A) {
      const imm = bytes[offset + 1];
      return { offset, size: 2, mnemonic: 'PUSH', operands: X86Decoder.formatHex8(imm) };
    }
    if (b0 === 0x68) {
      const imm = bytes[offset + 1] | (bytes[offset + 2] << 8);
      return { offset, size: 3, mnemonic: 'PUSH', operands: X86Decoder.formatHex16(imm) };
    }

    // ---------- LEA (8D) ----------
    if (b0 === 0x8D) {
      return X86Decoder.decodeModrmInstruction(bytes, offset, 'LEA', false, false);
    }

    // ---------- ALU r/m, r (00-03, 08-0B, 20-23, 28-2B, 30-33, 38-3B) ----------
    if (X86Decoder.isAluOpcode(b0)) {
      return X86Decoder.decodeAlu(bytes, offset, b0);
    }

    // ---------- MOV r/m ↔ r (88-8B) ----------
    if (b0 >= 0x88 && b0 <= 0x8B) {
      const isByte = (b0 & 0x01) === 0;
      const direction = (b0 & 0x02) !== 0; // 1 = reg is destination
      const label = direction ? 'dst' : 'src';
      return X86Decoder.decodeModrmInstruction(bytes, offset, 'MOV', isByte, direction, label);
    }

    // ---------- MOV m, imm (C6, C7) ----------
    if (b0 === 0xC6 || b0 === 0xC7) {
      return X86Decoder.decodeMovMemImm(bytes, offset, b0 === 0xC6);
    }

    // ---------- ALU r/m, imm (80, 81, 83) ----------
    if (b0 === 0x80 || b0 === 0x81 || b0 === 0x83) {
      return X86Decoder.decodeAluImm(bytes, offset, b0);
    }

    // ---------- XCHG r/m, r (86, 87) ----------
    if (b0 === 0x86 || b0 === 0x87) {
      const isByte = b0 === 0x86;
      return X86Decoder.decodeModrmInstruction(bytes, offset, 'XCHG', isByte, true);
    }

    // ---------- Shift group (D0-D3) ----------
    if (b0 >= 0xD0 && b0 <= 0xD3) {
      return X86Decoder.decodeShift(bytes, offset, b0);
    }

    // ---------- Unary group F6/F7 (NEG, NOT, MUL, DIV, TEST, etc.) ----------
    if (b0 === 0xF6 || b0 === 0xF7) {
      return X86Decoder.decodeUnaryGroup3(bytes, offset, b0 === 0xF6);
    }

    // ---------- Group FE/FF (INC/DEC mem, CALL/JMP indirect, PUSH mem) ----------
    if (b0 === 0xFE || b0 === 0xFF) {
      return X86Decoder.decodeGroupFE(bytes, offset, b0 === 0xFE);
    }

    // ---------- POP r/m16 (8F) ----------
    if (b0 === 0x8F) {
      return X86Decoder.decodeModrmInstruction(bytes, offset, 'POP', false, false, 'rm_only');
    }

    // ---------- Unrecognised opcode → DB ----------
    return { offset, size: 1, mnemonic: 'DB', operands: X86Decoder.formatHex8(b0) };
  }

  // ==================== ALU decoding ====================

  private static isAluOpcode(opcode: number): boolean {
    const base = opcode & 0xF8;
    if (opcode - base > 3) return false;
    return [0x00, 0x08, 0x20, 0x28, 0x30, 0x38].includes(base);
  }

  private static aluMnemonic(base: number): string {
    const map: Record<number, string> = {
      0x00: 'ADD', 0x08: 'OR', 0x20: 'AND', 0x28: 'SUB', 0x30: 'XOR', 0x38: 'CMP',
    };
    return map[base] ?? 'DB';
  }

  private static decodeAlu(bytes: Uint8Array, offset: number, opcode: number): DecodedEntry {
    const base = opcode & 0xF8;
    const mnemonic = X86Decoder.aluMnemonic(base);
    const isByte = (opcode & 0x01) === 0;
    const direction = (opcode & 0x02) !== 0;
    return X86Decoder.decodeModrmInstruction(bytes, offset, mnemonic, isByte, direction);
  }

  private static decodeAluImm(bytes: Uint8Array, offset: number, opcode: number): DecodedEntry {
    const modrm = bytes[offset + 1];
    const { mod, reg: ext, rm } = ModrmUtil.decode(modrm);
    const isByte = opcode === 0x80;
    const isSignExt = opcode === 0x83;

    const ALU_EXT: readonly string[] = ['ADD', 'OR', 'ADC', 'SBB', 'AND', 'SUB', 'XOR', 'CMP'];
    const mnemonic = ALU_EXT[ext];

    let pos = offset + 2;
    let rmStr: string;

    if (mod === 3) {
      rmStr = isByte ? ModrmUtil.byteRegName(rm) : ModrmUtil.wordRegName(rm);
    } else {
      const mem = ModrmUtil.decodeMemory(mod, rm, bytes, pos);
      rmStr = X86Decoder.formatMemory(mem.base, mem.displacement, isByte);
      pos += mem.bytesConsumed;
    }

    let immStr: string;
    if (isByte || isSignExt) {
      immStr = X86Decoder.formatHex8(bytes[pos]);
      pos += 1;
    } else {
      const imm16 = bytes[pos] | (bytes[pos + 1] << 8);
      immStr = X86Decoder.formatHex16(imm16);
      pos += 2;
    }

    return { offset, size: pos - offset, mnemonic, operands: `${rmStr}, ${immStr}` };
  }

  // ==================== ModR/M instruction decoding ====================

  private static decodeModrmInstruction(
    bytes: Uint8Array,
    offset: number,
    mnemonic: string,
    isByte: boolean,
    regIsDst: boolean,
    mode?: string,
  ): DecodedEntry {
    const modrm = bytes[offset + 1];
    const { mod, reg, rm } = ModrmUtil.decode(modrm);
    let pos = offset + 2;

    const regStr = isByte ? ModrmUtil.byteRegName(reg) : ModrmUtil.wordRegName(reg);

    let rmStr: string;
    if (mod === 3) {
      rmStr = isByte ? ModrmUtil.byteRegName(rm) : ModrmUtil.wordRegName(rm);
    } else {
      const mem = ModrmUtil.decodeMemory(mod, rm, bytes, pos);
      rmStr = X86Decoder.formatMemory(mem.base, mem.displacement, isByte);
      pos += mem.bytesConsumed;
    }

    let operands: string;
    if (mode === 'rm_only') {
      operands = rmStr;
    } else if (regIsDst) {
      operands = `${regStr}, ${rmStr}`;
    } else {
      operands = `${rmStr}, ${regStr}`;
    }

    return { offset, size: pos - offset, mnemonic, operands };
  }

  // ==================== MOV mem, imm ====================

  private static decodeMovMemImm(bytes: Uint8Array, offset: number, isByte: boolean): DecodedEntry {
    const modrm = bytes[offset + 1];
    const { mod, rm } = ModrmUtil.decode(modrm);
    let pos = offset + 2;

    let memStr: string;
    if (mod === 3) {
      memStr = isByte ? ModrmUtil.byteRegName(rm) : ModrmUtil.wordRegName(rm);
    } else {
      const mem = ModrmUtil.decodeMemory(mod, rm, bytes, pos);
      memStr = X86Decoder.formatMemory(mem.base, mem.displacement, isByte);
      pos += mem.bytesConsumed;
    }

    let immStr: string;
    if (isByte) {
      immStr = X86Decoder.formatHex8(bytes[pos]);
      pos += 1;
    } else {
      const imm16 = bytes[pos] | (bytes[pos + 1] << 8);
      immStr = X86Decoder.formatHex16(imm16);
      pos += 2;
    }

    return { offset, size: pos - offset, mnemonic: 'MOV', operands: `${memStr}, ${immStr}` };
  }

  // ==================== Shift group ====================

  private static decodeShift(bytes: Uint8Array, offset: number, opcode: number): DecodedEntry {
    const isByte = (opcode & 0x01) === 0;
    const byCl = (opcode & 0x02) !== 0;
    const modrm = bytes[offset + 1];
    const { mod, reg: ext, rm } = ModrmUtil.decode(modrm);

    const SHIFT_MNEMONICS: readonly string[] = ['ROL', 'ROR', 'RCL', 'RCR', 'SHL', 'SHR', 'SAL', 'SAR'];
    const mnemonic = SHIFT_MNEMONICS[ext];

    let pos = offset + 2;
    let rmStr: string;

    if (mod === 3) {
      rmStr = isByte ? ModrmUtil.byteRegName(rm) : ModrmUtil.wordRegName(rm);
    } else {
      const mem = ModrmUtil.decodeMemory(mod, rm, bytes, pos);
      rmStr = X86Decoder.formatMemory(mem.base, mem.displacement, isByte);
      pos += mem.bytesConsumed;
    }

    const countStr = byCl ? 'CL' : '1';
    return { offset, size: pos - offset, mnemonic, operands: `${rmStr}, ${countStr}` };
  }

  // ==================== Unary Group 3 (F6/F7) ====================

  private static decodeUnaryGroup3(bytes: Uint8Array, offset: number, isByte: boolean): DecodedEntry {
    const modrm = bytes[offset + 1];
    const { mod, reg: ext, rm } = ModrmUtil.decode(modrm);

    const GROUP3_MNEMONICS: readonly string[] = ['TEST', 'TEST', 'NOT', 'NEG', 'MUL', 'IMUL', 'DIV', 'IDIV'];
    const mnemonic = GROUP3_MNEMONICS[ext];

    let pos = offset + 2;
    let rmStr: string;

    if (mod === 3) {
      rmStr = isByte ? ModrmUtil.byteRegName(rm) : ModrmUtil.wordRegName(rm);
    } else {
      const mem = ModrmUtil.decodeMemory(mod, rm, bytes, pos);
      rmStr = X86Decoder.formatMemory(mem.base, mem.displacement, isByte);
      pos += mem.bytesConsumed;
    }

    // TEST has an immediate operand
    if (ext === 0) {
      if (isByte) {
        const imm = bytes[pos];
        pos += 1;
        return { offset, size: pos - offset, mnemonic, operands: `${rmStr}, ${X86Decoder.formatHex8(imm)}` };
      }
      const imm = bytes[pos] | (bytes[pos + 1] << 8);
      pos += 2;
      return { offset, size: pos - offset, mnemonic, operands: `${rmStr}, ${X86Decoder.formatHex16(imm)}` };
    }

    return { offset, size: pos - offset, mnemonic, operands: rmStr };
  }

  // ==================== Group FE/FF ====================

  private static decodeGroupFE(bytes: Uint8Array, offset: number, isByte: boolean): DecodedEntry {
    const modrm = bytes[offset + 1];
    const { mod, reg: ext, rm } = ModrmUtil.decode(modrm);

    let pos = offset + 2;
    let rmStr: string;

    if (mod === 3) {
      rmStr = isByte ? ModrmUtil.byteRegName(rm) : ModrmUtil.wordRegName(rm);
    } else {
      const mem = ModrmUtil.decodeMemory(mod, rm, bytes, pos);
      rmStr = X86Decoder.formatMemory(mem.base, mem.displacement, isByte);
      pos += mem.bytesConsumed;
    }

    if (ext === 0) return { offset, size: pos - offset, mnemonic: 'INC', operands: rmStr };
    if (ext === 1) return { offset, size: pos - offset, mnemonic: 'DEC', operands: rmStr };
    if (ext === 2) return { offset, size: pos - offset, mnemonic: 'CALL', operands: rmStr };
    if (ext === 4) return { offset, size: pos - offset, mnemonic: 'JMP', operands: rmStr };
    if (ext === 6) return { offset, size: pos - offset, mnemonic: 'PUSH', operands: rmStr };

    return { offset, size: pos - offset, mnemonic: 'DB', operands: X86Decoder.formatHex8(bytes[offset]) };
  }

  // ==================== String data detection ====================

  /**
   * Detect runs of printable ASCII ending with '$' (DOS string convention).
   * Returns array of [start, endExclusive] offsets.
   */
  private static detectStringData(bytes: Uint8Array): Array<[number, number]> {
    const regions: Array<[number, number]> = [];
    let i = 0;

    while (i < bytes.length) {
      // Look for '$'-terminated printable runs of at least 2 characters
      if (X86Decoder.isPrintable(bytes[i])) {
        const start = i;
        while (i < bytes.length && X86Decoder.isPrintable(bytes[i])) {
          i++;
        }
        // Check if terminated by '$'
        if (i > start + 1 && i - 1 < bytes.length && bytes[i - 1] === 0x24) {
          regions.push([start, i]);
        }
      } else {
        i++;
      }
    }

    return regions;
  }

  private static isPrintable(byte: number): boolean {
    return byte >= 0x20 && byte <= 0x7E;
  }

  /**
   * Merge decoded instruction entries with detected string data regions.
   * If an instruction overlaps a data region, replace it with DB directives.
   */
  private mergeDataRegions(
    entries: DecodedEntry[],
    dataRegions: Array<[number, number]>,
    bytes: Uint8Array,
  ): DecodedEntry[] {
    if (dataRegions.length === 0) return entries;

    const result: DecodedEntry[] = [];

    for (const entry of entries) {
      const entryEnd = entry.offset + entry.size;
      let isData = false;

      for (const [dStart, dEnd] of dataRegions) {
        // If entry falls within a data region and isn't already a DB
        if (entry.offset >= dStart && entryEnd <= dEnd && entry.mnemonic !== 'DB') {
          isData = true;
          break;
        }
      }

      if (isData) {
        // Emit as DB with the raw bytes
        const rawBytes = Array.from(bytes.slice(entry.offset, entryEnd));
        const operands = rawBytes.map(b => X86Decoder.formatHex8(b)).join(', ');
        result.push({ offset: entry.offset, size: entry.size, mnemonic: 'DB', operands });
      } else {
        result.push(entry);
      }
    }

    return result;
  }

  // ==================== Formatting ====================

  private static formatSource(entries: DecodedEntry[], branchTargets: Set<number>): string {
    const lines: string[] = [];
    lines.push('.MODEL SMALL');
    lines.push('.STACK 256');
    lines.push('.CODE');
    lines.push('');

    for (const entry of entries) {
      // Emit label if this offset is a branch target
      if (branchTargets.has(entry.offset)) {
        lines.push(`${X86Decoder.labelName(entry.offset)}:`);
      }

      const operandStr = entry.operands ? `  ${entry.operands}` : '';
      lines.push(`    ${entry.mnemonic}${operandStr}`);
    }

    lines.push('');
    return lines.join('\n');
  }

  private static labelName(offset: number): string {
    return `L_${offset.toString(16).toUpperCase().padStart(4, '0')}`;
  }

  private static formatHex8(value: number): string {
    const hex = (value & 0xFF).toString(16).toUpperCase().padStart(2, '0');
    return `0${hex}h`;
  }

  private static formatHex16(value: number): string {
    const hex = (value & 0xFFFF).toString(16).toUpperCase().padStart(4, '0');
    return `0${hex}h`;
  }

  private static formatMemory(base: string, displacement: number, isByte: boolean): string {
    const sizePrefix = isByte ? 'BYTE PTR ' : '';

    if (base === '') {
      // Direct addressing
      return `${sizePrefix}[${X86Decoder.formatHex16(displacement)}]`;
    }

    if (displacement === 0) {
      return `${sizePrefix}[${base}]`;
    }

    if (displacement > 0) {
      return `${sizePrefix}[${base}+${displacement}]`;
    }

    return `${sizePrefix}[${base}${displacement}]`;
  }

  private static signExtend8(value: number): number {
    return value >= 0x80 ? value - 0x100 : value;
  }

  private static signExtend16(value: number): number {
    return value >= 0x8000 ? value - 0x10000 : value;
  }

  private static condJumpMnemonic(opcode: number): string {
    const map: Record<number, string> = {
      0x70: 'JO', 0x71: 'JNO', 0x72: 'JB', 0x73: 'JNB',
      0x74: 'JE', 0x75: 'JNE', 0x76: 'JBE', 0x77: 'JA',
      0x78: 'JS', 0x79: 'JNS', 0x7A: 'JP', 0x7B: 'JNP',
      0x7C: 'JL', 0x7D: 'JGE', 0x7E: 'JLE', 0x7F: 'JG',
    };
    return map[opcode] ?? 'J??';
  }
}
