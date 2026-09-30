/**
 * X86 machine-code encoder for the Stack86 instruction subset.
 *
 * Takes a ParseResult (from the assembler parser) and produces a flat
 * binary suitable for a DOS .COM file (ORG 100h).
 *
 * Two-pass encoding:
 *   Pass 1 — encode each instruction to compute real byte sizes, build address map
 *   Pass 2 — re-encode with resolved label addresses (fixes relative jumps)
 */

import { ParsedInstruction, Operand, OperandType, OperandSize } from '../instruction/instruction.model';
import { ParseResult } from '../parser/parser';
import { ModrmUtil } from './modrm.util';

/** Result returned by the encoder. */
export interface EncodeResult {
  /** Flat binary bytes (COM format, ORG 100h). */
  readonly bytes: Uint8Array;
  /** Map from original instruction address → binary offset (relative to ORG). */
  readonly addressMap: ReadonlyMap<number, number>;
}

/** Intermediate representation of one encoded instruction. */
interface EncodedEntry {
  /** Original ParsedInstruction (null for raw data directives). */
  readonly instruction: ParsedInstruction;
  /** Encoded bytes (may be re-encoded in pass 2). */
  bytes: number[];
  /** Binary offset from start of the COM image (i.e. relative to ORG 100h). */
  offset: number;
}

export class X86Encoder {
  /** COM origin address. */
  private static readonly ORG = 0x0100;

  /**
   * Encode a parsed assembly program into a flat COM binary.
   *
   * @param result - ParseResult from the assembler parser.
   * @returns EncodeResult with the binary bytes and address map.
   */
  public encode(result: ParseResult): EncodeResult {
    const allInstructions = result.instructions;
    const labels = result.labels;

    // --- Pass 1: encode every instruction, compute byte sizes & offsets ---
    const entries: EncodedEntry[] = [];
    let offset = 0;

    for (const instr of allInstructions) {
      const bytes = this.encodeInstruction(instr, labels, offset);
      entries.push({ instruction: instr, bytes, offset });
      offset += bytes.length;
    }

    // Build address map: original emulator address → binary offset
    const addressMap = new Map<number, number>();
    for (const entry of entries) {
      addressMap.set(entry.instruction.address, entry.offset);
    }

    // --- Pass 2: re-encode instructions that have label-relative operands ---
    for (const entry of entries) {
      const instr = entry.instruction;
      if (X86Encoder.needsRelocationPass(instr)) {
        entry.bytes = this.encodeInstruction(instr, labels, entry.offset, addressMap);
      }
    }

    // Assemble final binary
    const totalSize = entries.reduce((sum, e) => sum + e.bytes.length, 0);
    const binary = new Uint8Array(totalSize);
    for (const entry of entries) {
      for (let i = 0; i < entry.bytes.length; i++) {
        binary[entry.offset + i] = entry.bytes[i] & 0xFF;
      }
    }

    return { bytes: binary, addressMap };
  }

  /**
   * Encode a single instruction into machine code bytes.
   */
  private encodeInstruction(
    instr: ParsedInstruction,
    labels: Map<string, number>,
    currentOffset: number,
    addressMap?: ReadonlyMap<number, number>,
  ): number[] {
    const mnemonic = instr.mnemonic.toUpperCase();
    const ops = instr.operands;

    switch (mnemonic) {
      case 'DB': return X86Encoder.encodeDb(ops);
      case 'DW': return X86Encoder.encodeDw(ops);
      case 'MOV': return this.encodeMov(ops);
      case 'ADD': return this.encodeAlu(ops, 0x00, 0);
      case 'OR':  return this.encodeAlu(ops, 0x08, 1);
      case 'AND': return this.encodeAlu(ops, 0x20, 4);
      case 'SUB': return this.encodeAlu(ops, 0x28, 5);
      case 'XOR': return this.encodeAlu(ops, 0x30, 6);
      case 'CMP': return this.encodeAlu(ops, 0x38, 7);
      case 'INC': return X86Encoder.encodeIncDec(ops, 0);
      case 'DEC': return X86Encoder.encodeIncDec(ops, 1);
      case 'NEG': return X86Encoder.encodeUnaryGroup3(ops, 3);
      case 'NOT': return X86Encoder.encodeUnaryGroup3(ops, 2);
      case 'MUL': return X86Encoder.encodeUnaryGroup3(ops, 4);
      case 'DIV': return X86Encoder.encodeUnaryGroup3(ops, 6);
      case 'SHL': case 'SAL': return X86Encoder.encodeShift(ops, 4);
      case 'SHR': return X86Encoder.encodeShift(ops, 5);
      case 'PUSH': return X86Encoder.encodePush(ops);
      case 'POP': return X86Encoder.encodePop(ops);
      case 'XCHG': return X86Encoder.encodeXchg(ops);
      case 'LEA': return X86Encoder.encodeLea(ops);
      case 'JMP': return this.encodeJmp(ops, labels, currentOffset, addressMap);
      case 'CALL': return this.encodeCall(ops, labels, currentOffset, addressMap);
      case 'RET': return [0xC3];
      case 'LOOP': return this.encodeCondJump(ops, 0xE2, labels, currentOffset, addressMap);
      case 'NOP': return [0x90];
      case 'HLT': return [0xF4];
      case 'INT': return X86Encoder.encodeInt(ops);
      // Conditional jumps
      case 'JE': case 'JZ':   return this.encodeCondJump(ops, 0x74, labels, currentOffset, addressMap);
      case 'JNE': case 'JNZ': return this.encodeCondJump(ops, 0x75, labels, currentOffset, addressMap);
      case 'JG':               return this.encodeCondJump(ops, 0x7F, labels, currentOffset, addressMap);
      case 'JGE':              return this.encodeCondJump(ops, 0x7D, labels, currentOffset, addressMap);
      case 'JL':               return this.encodeCondJump(ops, 0x7C, labels, currentOffset, addressMap);
      case 'JLE':              return this.encodeCondJump(ops, 0x7E, labels, currentOffset, addressMap);
      case 'JA':               return this.encodeCondJump(ops, 0x77, labels, currentOffset, addressMap);
      case 'JB': case 'JC':   return this.encodeCondJump(ops, 0x72, labels, currentOffset, addressMap);
      default:
        // Unknown instruction — encode as NOP to maintain stream position
        return [0x90];
    }
  }

  // ---------- Data directives ----------

  private static encodeDb(ops: Operand[]): number[] {
    const bytes: number[] = [];
    for (const op of ops) {
      if (typeof op.value === 'string') {
        for (let i = 0; i < op.value.length; i++) {
          bytes.push(op.value.charCodeAt(i) & 0xFF);
        }
      } else {
        bytes.push((op.value as number) & 0xFF);
      }
    }
    return bytes.length > 0 ? bytes : [0];
  }

  private static encodeDw(ops: Operand[]): number[] {
    const bytes: number[] = [];
    for (const op of ops) {
      const val = typeof op.value === 'number' ? op.value : 0;
      bytes.push(val & 0xFF, (val >> 8) & 0xFF);
    }
    return bytes.length > 0 ? bytes : [0, 0];
  }

  // ---------- MOV ----------

  private encodeMov(ops: Operand[]): number[] {
    const [dst, src] = ops;

    // MOV reg, imm → B0+rb (byte) or B8+rw (word)
    if (dst.type === OperandType.Register && src.type === OperandType.Immediate) {
      const regName = (dst.value as string).toLowerCase();
      const immVal = src.value as number;
      if (ModrmUtil.isByteRegister(regName)) {
        return [0xB0 + ModrmUtil.byteRegCode(regName), immVal & 0xFF];
      }
      const code = ModrmUtil.wordRegCode(regName);
      return [0xB8 + code, immVal & 0xFF, (immVal >> 8) & 0xFF];
    }

    // MOV reg, reg
    if (dst.type === OperandType.Register && src.type === OperandType.Register) {
      const isByte = ModrmUtil.isByteRegister((dst.value as string).toLowerCase());
      const opcode = isByte ? 0x8A : 0x8B;
      const dstCode = ModrmUtil.registerCode((dst.value as string));
      return [opcode, ...ModrmUtil.encodeRegister(dstCode, src.value as string, isByte)];
    }

    // MOV reg, mem
    if (dst.type === OperandType.Register && src.type === OperandType.Memory) {
      const isByte = ModrmUtil.isByteRegister((dst.value as string).toLowerCase());
      const opcode = isByte ? 0x8A : 0x8B;
      const regCode = ModrmUtil.registerCode(dst.value as string);
      return [opcode, ...X86Encoder.encodeMemOperand(regCode, src)];
    }

    // MOV mem, reg
    if (dst.type === OperandType.Memory && src.type === OperandType.Register) {
      const isByte = ModrmUtil.isByteRegister((src.value as string).toLowerCase());
      const opcode = isByte ? 0x88 : 0x89;
      const regCode = ModrmUtil.registerCode(src.value as string);
      return [opcode, ...X86Encoder.encodeMemOperand(regCode, dst)];
    }

    // MOV mem, imm
    if (dst.type === OperandType.Memory && src.type === OperandType.Immediate) {
      const isByte = dst.size === OperandSize.Byte;
      const opcode = isByte ? 0xC6 : 0xC7;
      const memBytes = X86Encoder.encodeMemOperand(0, dst);
      const immVal = src.value as number;
      if (isByte) {
        return [opcode, ...memBytes, immVal & 0xFF];
      }
      return [opcode, ...memBytes, immVal & 0xFF, (immVal >> 8) & 0xFF];
    }

    return [0x90]; // fallback NOP
  }

  // ---------- ALU (ADD, OR, AND, SUB, XOR, CMP) ----------

  private encodeAlu(ops: Operand[], baseOpcode: number, extCode: number): number[] {
    const [dst, src] = ops;

    // ALU reg, imm → 80/81/83 /ext
    if (dst.type === OperandType.Register && src.type === OperandType.Immediate) {
      const regName = (dst.value as string).toLowerCase();
      const immVal = src.value as number;

      if (ModrmUtil.isByteRegister(regName)) {
        const modrm = ModrmUtil.encodeRegister(extCode, regName, true);
        return [0x80, ...modrm, immVal & 0xFF];
      }

      // Word register: use 83h (sign-extended imm8) when possible
      if (immVal >= -128 && immVal <= 127) {
        const modrm = ModrmUtil.encodeRegister(extCode, regName, false);
        return [0x83, ...modrm, immVal & 0xFF];
      }

      const modrm = ModrmUtil.encodeRegister(extCode, regName, false);
      return [0x81, ...modrm, immVal & 0xFF, (immVal >> 8) & 0xFF];
    }

    // ALU reg, reg
    if (dst.type === OperandType.Register && src.type === OperandType.Register) {
      const isByte = ModrmUtil.isByteRegister((dst.value as string).toLowerCase());
      const opcode = baseOpcode + (isByte ? 2 : 3);
      const dstCode = ModrmUtil.registerCode(dst.value as string);
      return [opcode, ...ModrmUtil.encodeRegister(dstCode, src.value as string, isByte)];
    }

    // ALU reg, mem
    if (dst.type === OperandType.Register && src.type === OperandType.Memory) {
      const isByte = ModrmUtil.isByteRegister((dst.value as string).toLowerCase());
      const opcode = baseOpcode + (isByte ? 2 : 3);
      const regCode = ModrmUtil.registerCode(dst.value as string);
      return [opcode, ...X86Encoder.encodeMemOperand(regCode, src)];
    }

    // ALU mem, reg
    if (dst.type === OperandType.Memory && src.type === OperandType.Register) {
      const isByte = ModrmUtil.isByteRegister((src.value as string).toLowerCase());
      const opcode = baseOpcode + (isByte ? 0 : 1);
      const regCode = ModrmUtil.registerCode(src.value as string);
      return [opcode, ...X86Encoder.encodeMemOperand(regCode, dst)];
    }

    return [0x90];
  }

  // ---------- INC / DEC ----------

  private static encodeIncDec(ops: Operand[], code: number): number[] {
    const op = ops[0];
    if (op.type === OperandType.Register) {
      const regName = (op.value as string).toLowerCase();
      if (ModrmUtil.isByteRegister(regName)) {
        const opcode = code === 0 ? 0xFE : 0xFE;
        return [opcode, ...ModrmUtil.encodeRegister(code, regName, true)];
      }
      // 16-bit: 40+rw (INC) / 48+rw (DEC)
      const regCode = ModrmUtil.wordRegCode(regName);
      return [0x40 + code * 8 + regCode];
    }
    if (op.type === OperandType.Memory) {
      const isByte = op.size === OperandSize.Byte;
      const opcode = isByte ? 0xFE : 0xFF;
      return [opcode, ...X86Encoder.encodeMemOperand(code, op)];
    }
    return [0x90];
  }

  // ---------- Unary Group 3 (NEG, NOT, MUL, DIV) ----------

  private static encodeUnaryGroup3(ops: Operand[], extCode: number): number[] {
    const op = ops[0];
    if (op.type === OperandType.Register) {
      const regName = (op.value as string).toLowerCase();
      const isByte = ModrmUtil.isByteRegister(regName);
      const opcode = isByte ? 0xF6 : 0xF7;
      return [opcode, ...ModrmUtil.encodeRegister(extCode, regName, isByte)];
    }
    if (op.type === OperandType.Memory) {
      const isByte = op.size === OperandSize.Byte;
      const opcode = isByte ? 0xF6 : 0xF7;
      return [opcode, ...X86Encoder.encodeMemOperand(extCode, op)];
    }
    return [0x90];
  }

  // ---------- Shift (SHL, SHR) ----------

  private static encodeShift(ops: Operand[], extCode: number): number[] {
    const [dst, cnt] = ops;
    const dstName = (dst.value as string).toLowerCase();
    const isByte = dst.type === OperandType.Register && ModrmUtil.isByteRegister(dstName);

    // Shift by 1
    if (cnt.type === OperandType.Immediate && cnt.value === 1) {
      const opcode = isByte ? 0xD0 : 0xD1;
      if (dst.type === OperandType.Register) {
        return [opcode, ...ModrmUtil.encodeRegister(extCode, dstName, isByte)];
      }
      return [opcode, ...X86Encoder.encodeMemOperand(extCode, dst)];
    }

    // Shift by CL
    if (cnt.type === OperandType.Register && (cnt.value as string).toLowerCase() === 'cl') {
      const opcode = isByte ? 0xD2 : 0xD3;
      if (dst.type === OperandType.Register) {
        return [opcode, ...ModrmUtil.encodeRegister(extCode, dstName, isByte)];
      }
      return [opcode, ...X86Encoder.encodeMemOperand(extCode, dst)];
    }

    return [0x90];
  }

  // ---------- PUSH / POP ----------

  private static encodePush(ops: Operand[]): number[] {
    const op = ops[0];
    if (op.type === OperandType.Register) {
      const regName = (op.value as string).toLowerCase();
      const segCode = ModrmUtil.segRegCode(regName);
      if (segCode >= 0) {
        // PUSH seg: 06, 0E, 16, 1E
        return [0x06 + segCode * 8];
      }
      const code = ModrmUtil.wordRegCode(regName);
      return [0x50 + code];
    }
    if (op.type === OperandType.Immediate) {
      const val = op.value as number;
      if (val >= -128 && val <= 127) {
        return [0x6A, val & 0xFF];
      }
      return [0x68, val & 0xFF, (val >> 8) & 0xFF];
    }
    if (op.type === OperandType.Memory) {
      return [0xFF, ...X86Encoder.encodeMemOperand(6, op)];
    }
    return [0x90];
  }

  private static encodePop(ops: Operand[]): number[] {
    const op = ops[0];
    if (op.type === OperandType.Register) {
      const regName = (op.value as string).toLowerCase();
      const segCode = ModrmUtil.segRegCode(regName);
      if (segCode >= 0) {
        // POP seg: 07, 0F(?), 17, 1F — note: CS (0E) can't be popped
        return [0x07 + segCode * 8];
      }
      const code = ModrmUtil.wordRegCode(regName);
      return [0x58 + code];
    }
    if (op.type === OperandType.Memory) {
      return [0x8F, ...X86Encoder.encodeMemOperand(0, op)];
    }
    return [0x90];
  }

  // ---------- XCHG ----------

  private static encodeXchg(ops: Operand[]): number[] {
    const [dst, src] = ops;
    if (dst.type === OperandType.Register && src.type === OperandType.Register) {
      const dstName = (dst.value as string).toLowerCase();
      const srcName = (src.value as string).toLowerCase();
      const isByte = ModrmUtil.isByteRegister(dstName);

      // XCHG AX, reg16 → 90+rw (short form)
      if (!isByte && (dstName === 'ax' || srcName === 'ax')) {
        const other = dstName === 'ax' ? srcName : dstName;
        return [0x90 + ModrmUtil.wordRegCode(other)];
      }

      const opcode = isByte ? 0x86 : 0x87;
      const dstCode = ModrmUtil.registerCode(dstName);
      return [opcode, ...ModrmUtil.encodeRegister(dstCode, srcName, isByte)];
    }
    if (dst.type === OperandType.Register && src.type === OperandType.Memory) {
      const isByte = ModrmUtil.isByteRegister((dst.value as string).toLowerCase());
      const opcode = isByte ? 0x86 : 0x87;
      const regCode = ModrmUtil.registerCode(dst.value as string);
      return [opcode, ...X86Encoder.encodeMemOperand(regCode, src)];
    }
    return [0x90];
  }

  // ---------- LEA ----------

  private static encodeLea(ops: Operand[]): number[] {
    const [dst, src] = ops;
    if (dst.type === OperandType.Register && src.type === OperandType.Memory) {
      const regCode = ModrmUtil.wordRegCode((dst.value as string).toLowerCase());
      return [0x8D, ...X86Encoder.encodeMemOperand(regCode, src)];
    }
    return [0x90];
  }

  // ---------- JMP ----------

  private encodeJmp(
    ops: Operand[],
    labels: Map<string, number>,
    currentOffset: number,
    addressMap?: ReadonlyMap<number, number>,
  ): number[] {
    const op = ops[0];

    // JMP to label or immediate
    if (op.type === OperandType.Label || op.type === OperandType.Immediate) {
      const targetAddr = this.resolveTarget(op, labels);
      const targetOffset = addressMap?.get(targetAddr) ?? targetAddr;

      // Try short jump (rel8) first — instruction is 2 bytes
      const rel8 = targetOffset - (currentOffset + 2);
      if (rel8 >= -128 && rel8 <= 127) {
        return [0xEB, rel8 & 0xFF];
      }

      // Near jump (rel16) — instruction is 3 bytes
      const rel16 = targetOffset - (currentOffset + 3);
      return [0xE9, rel16 & 0xFF, (rel16 >> 8) & 0xFF];
    }

    // JMP reg
    if (op.type === OperandType.Register) {
      const regCode = ModrmUtil.wordRegCode((op.value as string).toLowerCase());
      return [0xFF, ModrmUtil.encode(0x03, 4, regCode)];
    }

    // JMP mem
    if (op.type === OperandType.Memory) {
      return [0xFF, ...X86Encoder.encodeMemOperand(4, op)];
    }

    return [0x90];
  }

  // ---------- Conditional jumps (Jcc) + LOOP ----------

  private encodeCondJump(
    ops: Operand[],
    opcode: number,
    labels: Map<string, number>,
    currentOffset: number,
    addressMap?: ReadonlyMap<number, number>,
  ): number[] {
    const op = ops[0];
    const targetAddr = this.resolveTarget(op, labels);
    const targetOffset = addressMap?.get(targetAddr) ?? targetAddr;

    // Conditional jumps are always rel8 (2-byte instruction)
    const rel = targetOffset - (currentOffset + 2);
    return [opcode, rel & 0xFF];
  }

  // ---------- CALL ----------

  private encodeCall(
    ops: Operand[],
    labels: Map<string, number>,
    currentOffset: number,
    addressMap?: ReadonlyMap<number, number>,
  ): number[] {
    const op = ops[0];

    if (op.type === OperandType.Label || op.type === OperandType.Immediate) {
      const targetAddr = this.resolveTarget(op, labels);
      const targetOffset = addressMap?.get(targetAddr) ?? targetAddr;
      // CALL rel16 — 3-byte instruction
      const rel = targetOffset - (currentOffset + 3);
      return [0xE8, rel & 0xFF, (rel >> 8) & 0xFF];
    }

    if (op.type === OperandType.Register) {
      const regCode = ModrmUtil.wordRegCode((op.value as string).toLowerCase());
      return [0xFF, ModrmUtil.encode(0x03, 2, regCode)];
    }

    if (op.type === OperandType.Memory) {
      return [0xFF, ...X86Encoder.encodeMemOperand(2, op)];
    }

    return [0x90];
  }

  // ---------- INT ----------

  private static encodeInt(ops: Operand[]): number[] {
    const val = (ops[0].value as number) & 0xFF;
    return [0xCD, val];
  }

  // ---------- Helpers ----------

  /**
   * Encode memory operand to ModR/M + displacement bytes.
   * Handles direct addressing, register-indirect, and offset addressing.
   */
  private static encodeMemOperand(regField: number, op: Operand): number[] {
    const val = op.value;
    const displacement = op.offset ?? 0;

    // Direct addressing: [nnnn] where value is a number
    if (typeof val === 'number') {
      return ModrmUtil.encodeMemory(regField, '', val + displacement);
    }

    // Register indirect: [BX], [SI], [BP+SI], etc.
    const inner = (val as string).toLowerCase();
    return ModrmUtil.encodeMemory(regField, inner, displacement);
  }

  /** Resolve a label/immediate operand to its emulator address. */
  private resolveTarget(op: Operand, labels: Map<string, number>): number {
    if (op.type === OperandType.Label) {
      const addr = labels.get(op.value as string);
      if (addr === undefined) {
        throw new Error(`Undefined label in encoder: ${op.value}`);
      }
      return addr;
    }
    return op.value as number;
  }

  /** Check whether an instruction uses labels that need relocation. */
  private static needsRelocationPass(instr: ParsedInstruction): boolean {
    const m = instr.mnemonic.toUpperCase();
    const flowInstructions = [
      'JMP', 'JE', 'JZ', 'JNE', 'JNZ', 'JG', 'JGE', 'JL', 'JLE',
      'JA', 'JB', 'JC', 'CALL', 'LOOP',
    ];
    return flowInstructions.includes(m);
  }
}
