import { ExecutionContext, Operand, OperandType } from '../instruction.model';
import { OperandHelper } from '../operand.util';
import { CpuHelper } from '../../cpu/cpu.model';
import { Int86Service } from './int86-services';
import { StackAccessType, IoType } from '../../execution/execution-trace.model';

export class DataHandler {
  public static push(ctx: ExecutionContext, ops: Operand[]): void {
    DataHandler.assertOperandCount('PUSH', ops, 1);
    if (ctx.trace) ctx.trace.stackAccess = StackAccessType.Push;
    const value = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    ctx.cpu.sp = CpuHelper.toWord(ctx.cpu.sp - 2);
    ctx.memory.writeWord(ctx.cpu.sp, CpuHelper.toWord(value));
  }

  public static pop(ctx: ExecutionContext, ops: Operand[]): void {
    DataHandler.assertOperandCount('POP', ops, 1);
    if (ctx.trace) ctx.trace.stackAccess = StackAccessType.Pop;
    const value = ctx.memory.readWord(ctx.cpu.sp);
    ctx.cpu.sp = CpuHelper.toWord(ctx.cpu.sp + 2);
    OperandHelper.write(ctx.cpu, ctx.memory, ops[0], value, ctx.labels, ctx.trace);
  }

  public static xchg(ctx: ExecutionContext, ops: Operand[]): void {
    DataHandler.assertOperandCount('XCHG', ops, 2);
    const a = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    const b = OperandHelper.read(ctx.cpu, ctx.memory, ops[1], ctx.labels, ctx.trace);
    OperandHelper.write(ctx.cpu, ctx.memory, ops[0], b, ctx.labels, ctx.trace);
    OperandHelper.write(ctx.cpu, ctx.memory, ops[1], a, ctx.labels, ctx.trace);
  }

  public static lea(ctx: ExecutionContext, ops: Operand[]): void {
    DataHandler.assertOperandCount('LEA', ops, 2);
    if (ops[1].type === OperandType.Memory) {
      const addr = typeof ops[1].value === 'number'
        ? ops[1].value
        : (ctx.labels.get(ops[1].value as string) ?? parseInt(ops[1].value as string, 16));
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], CpuHelper.toWord(addr), ctx.labels, ctx.trace);
    } else {
      const value = OperandHelper.read(ctx.cpu, ctx.memory, ops[1], ctx.labels, ctx.trace);
      OperandHelper.write(ctx.cpu, ctx.memory, ops[0], value, ctx.labels, ctx.trace);
    }
  }

  public static nop(): void {
    // Do nothing
  }

  public static hlt(ctx: ExecutionContext): void {
    ctx.halted = true;
  }

  public static int(ctx: ExecutionContext, ops: Operand[]): void {
    DataHandler.assertOperandCount('INT', ops, 1);
    const intNum = OperandHelper.read(ctx.cpu, ctx.memory, ops[0], ctx.labels, ctx.trace);
    if (intNum === 0x21) {
      DataHandler.handleInt21(ctx);
    } else if (intNum === 0x86) {
      DataHandler.handleInt86(ctx);
    }
  }

  private static handleInt21(ctx: ExecutionContext): void {
    const ah = (ctx.cpu.ax >> 8) & 0xFF;
    switch (ah) {
      case 0x01: {
        if (ctx.trace) ctx.trace.ioType = IoType.Input;
        if (ctx.onInput) {
          const input = ctx.onInput();
          if (input && input.length > 0) {
            const ch = input.charCodeAt(0) & 0xFF;
            ctx.cpu.ax = (ctx.cpu.ax & 0xFF00) | ch;
            if (ctx.onOutput) ctx.onOutput(input[0]);
          }
        }
        break;
      }
      case 0x02: {
        if (ctx.trace) ctx.trace.ioType = IoType.Output;
        const dl = ctx.cpu.dx & 0xFF;
        if (ctx.onOutput) ctx.onOutput(String.fromCharCode(dl));
        break;
      }
      case 0x09: {
        if (ctx.trace) ctx.trace.ioType = IoType.Output;
        const startAddr = ctx.cpu.dx;
        let output = '';
        let addr = startAddr;
        while (addr < 0x10000) {
          const ch = ctx.memory.readByte(addr);
          if (ch === 0x24) break;
          output += String.fromCharCode(ch);
          addr++;
        }
        if (ctx.onOutput) ctx.onOutput(output);
        break;
      }
      case 0x4C: {
        ctx.halted = true;
        break;
      }
    }
  }

  private static handleInt86(ctx: ExecutionContext): void {
    const ah = (ctx.cpu.ax >> 8) & 0xFF;
    // Arguments are on the stack (pushed right-to-left by caller).
    // SP currently points at the return address from the INT instruction context;
    // the pushed args start at SP+0 (first arg = last pushed = lowest address).
    switch (ah) {
      case Int86Service.Sleep: {
        const ms = ctx.memory.readWord(ctx.cpu.sp);
        ctx.sleepMs = ms;
        ctx.cpu.ax = 0;
        break;
      }
      case Int86Service.Strlen: {
        const addr = ctx.memory.readWord(ctx.cpu.sp);
        let len = 0;
        while (addr + len < 0x10000 && ctx.memory.readByte(addr + len) !== 0x24 && ctx.memory.readByte(addr + len) !== 0) {
          len++;
        }
        ctx.cpu.ax = CpuHelper.toWord(len);
        break;
      }
      case Int86Service.Strcpy: {
        const dst = ctx.memory.readWord(ctx.cpu.sp);
        const src = ctx.memory.readWord(ctx.cpu.sp + 2);
        let i = 0;
        while (src + i < 0x10000) {
          const ch = ctx.memory.readByte(src + i);
          ctx.memory.writeByte(dst + i, ch);
          if (ch === 0 || ch === 0x24) break;
          i++;
        }
        ctx.cpu.ax = CpuHelper.toWord(dst);
        break;
      }
      case Int86Service.Strcmp: {
        const s1 = ctx.memory.readWord(ctx.cpu.sp);
        const s2 = ctx.memory.readWord(ctx.cpu.sp + 2);
        let j = 0;
        let result = 0;
        while (s1 + j < 0x10000 && s2 + j < 0x10000) {
          const c1 = ctx.memory.readByte(s1 + j);
          const c2 = ctx.memory.readByte(s2 + j);
          if (c1 !== c2) {
            result = c1 < c2 ? -1 : 1;
            break;
          }
          if (c1 === 0 || c1 === 0x24) break;
          j++;
        }
        ctx.cpu.ax = CpuHelper.toWord(result);
        break;
      }
      case Int86Service.Memset: {
        const ptr = ctx.memory.readWord(ctx.cpu.sp);
        const val = ctx.memory.readWord(ctx.cpu.sp + 2) & 0xFF;
        const n = ctx.memory.readWord(ctx.cpu.sp + 4);
        for (let i = 0; i < n && ptr + i < 0x10000; i++) {
          ctx.memory.writeByte(ptr + i, val);
        }
        ctx.cpu.ax = CpuHelper.toWord(ptr);
        break;
      }
      case Int86Service.Rand: {
        ctx.cpu.ax = CpuHelper.toWord(Math.floor(Math.random() * 32768));
        break;
      }
      case Int86Service.Srand: {
        // Seed is accepted but JavaScript Math.random can't be seeded; no-op.
        ctx.cpu.ax = 0;
        break;
      }
      case Int86Service.Atoi: {
        const addr = ctx.memory.readWord(ctx.cpu.sp);
        let str = '';
        let k = 0;
        while (addr + k < 0x10000) {
          const ch = ctx.memory.readByte(addr + k);
          if (ch === 0 || ch === 0x24) break;
          str += String.fromCharCode(ch);
          k++;
        }
        const parsed = parseInt(str, 10);
        ctx.cpu.ax = CpuHelper.toWord(isNaN(parsed) ? 0 : parsed);
        break;
      }
      case Int86Service.Strcat: {
        const dst = ctx.memory.readWord(ctx.cpu.sp);
        const src = ctx.memory.readWord(ctx.cpu.sp + 2);
        // Find end of dst
        let dstEnd = dst;
        while (dstEnd < 0x10000 && ctx.memory.readByte(dstEnd) !== 0 && ctx.memory.readByte(dstEnd) !== 0x24) {
          dstEnd++;
        }
        // Copy src to end of dst
        let i = 0;
        while (src + i < 0x10000) {
          const ch = ctx.memory.readByte(src + i);
          ctx.memory.writeByte(dstEnd + i, ch);
          if (ch === 0 || ch === 0x24) break;
          i++;
        }
        ctx.cpu.ax = CpuHelper.toWord(dst);
        break;
      }
      case Int86Service.Memcpy: {
        const dst = ctx.memory.readWord(ctx.cpu.sp);
        const src = ctx.memory.readWord(ctx.cpu.sp + 2);
        const n = ctx.memory.readWord(ctx.cpu.sp + 4);
        for (let i = 0; i < n && src + i < 0x10000 && dst + i < 0x10000; i++) {
          ctx.memory.writeByte(dst + i, ctx.memory.readByte(src + i));
        }
        ctx.cpu.ax = CpuHelper.toWord(dst);
        break;
      }
      case Int86Service.Toupper: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        ctx.cpu.ax = (ch >= 0x61 && ch <= 0x7A) ? CpuHelper.toWord(ch - 0x20) : CpuHelper.toWord(ch);
        break;
      }
      case Int86Service.Tolower: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        ctx.cpu.ax = (ch >= 0x41 && ch <= 0x5A) ? CpuHelper.toWord(ch + 0x20) : CpuHelper.toWord(ch);
        break;
      }
      case Int86Service.Getchar: {
        if (ctx.onInput) {
          const input = ctx.onInput();
          if (input && input.length > 0) {
            ctx.cpu.ax = CpuHelper.toWord(input.charCodeAt(0) & 0xFF);
            if (ctx.onOutput) ctx.onOutput(input[0]);
          } else {
            ctx.cpu.ax = CpuHelper.toWord(0xFFFF); // EOF
          }
        } else {
          ctx.cpu.ax = CpuHelper.toWord(0xFFFF);
        }
        break;
      }
      case Int86Service.Exit: {
        ctx.halted = true;
        break;
      }

      // ── string.h extended ──────────────────────────────────────────
      case Int86Service.Strncpy: {
        const dst = ctx.memory.readWord(ctx.cpu.sp);
        const src = ctx.memory.readWord(ctx.cpu.sp + 2);
        const n = ctx.memory.readWord(ctx.cpu.sp + 4);
        let i = 0;
        while (i < n && src + i < 0x10000) {
          const ch = ctx.memory.readByte(src + i);
          ctx.memory.writeByte(dst + i, ch);
          if (ch === 0 || ch === 0x24) {
            i++;
            break;
          }
          i++;
        }
        // Pad remaining with zeros
        while (i < n && dst + i < 0x10000) {
          ctx.memory.writeByte(dst + i, 0);
          i++;
        }
        ctx.cpu.ax = CpuHelper.toWord(dst);
        break;
      }
      case Int86Service.Strncmp: {
        const s1 = ctx.memory.readWord(ctx.cpu.sp);
        const s2 = ctx.memory.readWord(ctx.cpu.sp + 2);
        const n = ctx.memory.readWord(ctx.cpu.sp + 4);
        let result = 0;
        for (let i = 0; i < n; i++) {
          const c1 = ctx.memory.readByte(s1 + i);
          const c2 = ctx.memory.readByte(s2 + i);
          if (c1 !== c2) {
            result = c1 < c2 ? -1 : 1;
            break;
          }
          if (c1 === 0 || c1 === 0x24) break;
        }
        ctx.cpu.ax = CpuHelper.toWord(result);
        break;
      }
      case Int86Service.Strncat: {
        const dst = ctx.memory.readWord(ctx.cpu.sp);
        const src = ctx.memory.readWord(ctx.cpu.sp + 2);
        const n = ctx.memory.readWord(ctx.cpu.sp + 4);
        // Find end of dst
        let dstEnd = dst;
        while (dstEnd < 0x10000 && ctx.memory.readByte(dstEnd) !== 0 && ctx.memory.readByte(dstEnd) !== 0x24) {
          dstEnd++;
        }
        // Append at most n chars from src
        let i = 0;
        while (i < n && src + i < 0x10000) {
          const ch = ctx.memory.readByte(src + i);
          if (ch === 0 || ch === 0x24) break;
          ctx.memory.writeByte(dstEnd + i, ch);
          i++;
        }
        // Null-terminate
        ctx.memory.writeByte(dstEnd + i, 0x24);
        ctx.cpu.ax = CpuHelper.toWord(dst);
        break;
      }
      case Int86Service.Strchr: {
        const s = ctx.memory.readWord(ctx.cpu.sp);
        const ch = ctx.memory.readWord(ctx.cpu.sp + 2) & 0xFF;
        let addr = s;
        let found = 0;
        while (addr < 0x10000) {
          const c = ctx.memory.readByte(addr);
          if (c === ch) {
            found = addr;
            break;
          }
          if (c === 0 || c === 0x24) break;
          addr++;
        }
        ctx.cpu.ax = CpuHelper.toWord(found);
        break;
      }
      case Int86Service.Strrchr: {
        const s = ctx.memory.readWord(ctx.cpu.sp);
        const ch = ctx.memory.readWord(ctx.cpu.sp + 2) & 0xFF;
        let addr = s;
        let last = 0;
        while (addr < 0x10000) {
          const c = ctx.memory.readByte(addr);
          if (c === ch) last = addr;
          if (c === 0 || c === 0x24) break;
          addr++;
        }
        ctx.cpu.ax = CpuHelper.toWord(last);
        break;
      }
      case Int86Service.Strstr: {
        const haystack = ctx.memory.readWord(ctx.cpu.sp);
        const needle = ctx.memory.readWord(ctx.cpu.sp + 2);
        // Read needle string
        const needleChars: number[] = [];
        let ni = 0;
        while (needle + ni < 0x10000) {
          const c = ctx.memory.readByte(needle + ni);
          if (c === 0 || c === 0x24) break;
          needleChars.push(c);
          ni++;
        }
        if (needleChars.length === 0) {
          ctx.cpu.ax = CpuHelper.toWord(haystack);
          break;
        }
        let found = 0;
        let hi = haystack;
        while (hi < 0x10000) {
          const c = ctx.memory.readByte(hi);
          if (c === 0 || c === 0x24) break;
          // Check for match
          let match = true;
          for (let j = 0; j < needleChars.length; j++) {
            if (hi + j >= 0x10000 || ctx.memory.readByte(hi + j) !== needleChars[j]) {
              match = false;
              break;
            }
          }
          if (match) {
            found = hi;
            break;
          }
          hi++;
        }
        ctx.cpu.ax = CpuHelper.toWord(found);
        break;
      }
      case Int86Service.Memcmp: {
        const s1 = ctx.memory.readWord(ctx.cpu.sp);
        const s2 = ctx.memory.readWord(ctx.cpu.sp + 2);
        const n = ctx.memory.readWord(ctx.cpu.sp + 4);
        let result = 0;
        for (let i = 0; i < n && s1 + i < 0x10000 && s2 + i < 0x10000; i++) {
          const b1 = ctx.memory.readByte(s1 + i);
          const b2 = ctx.memory.readByte(s2 + i);
          if (b1 !== b2) {
            result = b1 < b2 ? -1 : 1;
            break;
          }
        }
        ctx.cpu.ax = CpuHelper.toWord(result);
        break;
      }

      // ── ctype.h extended ───────────────────────────────────────────
      case Int86Service.Isalpha: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        ctx.cpu.ax = CpuHelper.toWord(((ch >= 0x41 && ch <= 0x5A) || (ch >= 0x61 && ch <= 0x7A)) ? 1 : 0);
        break;
      }
      case Int86Service.Isdigit: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        ctx.cpu.ax = CpuHelper.toWord((ch >= 0x30 && ch <= 0x39) ? 1 : 0);
        break;
      }
      case Int86Service.Isalnum: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        ctx.cpu.ax = CpuHelper.toWord(
          ((ch >= 0x41 && ch <= 0x5A) || (ch >= 0x61 && ch <= 0x7A) || (ch >= 0x30 && ch <= 0x39)) ? 1 : 0,
        );
        break;
      }
      case Int86Service.Isspace: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        ctx.cpu.ax = CpuHelper.toWord((ch === 0x20 || (ch >= 0x09 && ch <= 0x0D)) ? 1 : 0);
        break;
      }
      case Int86Service.Isupper: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        ctx.cpu.ax = CpuHelper.toWord((ch >= 0x41 && ch <= 0x5A) ? 1 : 0);
        break;
      }
      case Int86Service.Islower: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        ctx.cpu.ax = CpuHelper.toWord((ch >= 0x61 && ch <= 0x7A) ? 1 : 0);
        break;
      }
      case Int86Service.Ispunct: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        const isPrint = ch >= 0x21 && ch <= 0x7E;
        const isAlnum = (ch >= 0x41 && ch <= 0x5A) || (ch >= 0x61 && ch <= 0x7A) || (ch >= 0x30 && ch <= 0x39);
        ctx.cpu.ax = CpuHelper.toWord((isPrint && !isAlnum) ? 1 : 0);
        break;
      }
      case Int86Service.Isprint: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        ctx.cpu.ax = CpuHelper.toWord((ch >= 0x20 && ch <= 0x7E) ? 1 : 0);
        break;
      }
      case Int86Service.Isxdigit: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        ctx.cpu.ax = CpuHelper.toWord(
          ((ch >= 0x30 && ch <= 0x39) || (ch >= 0x41 && ch <= 0x46) || (ch >= 0x61 && ch <= 0x66)) ? 1 : 0,
        );
        break;
      }
      case Int86Service.Iscntrl: {
        const ch = ctx.memory.readWord(ctx.cpu.sp) & 0xFF;
        ctx.cpu.ax = CpuHelper.toWord((ch < 0x20 || ch === 0x7F) ? 1 : 0);
        break;
      }

      // ── stdlib.h heap ──────────────────────────────────────────────
      case Int86Service.Malloc: {
        const size = ctx.memory.readWord(ctx.cpu.sp);
        ctx.cpu.ax = CpuHelper.toWord(ctx.heap.allocate(size));
        break;
      }
      case Int86Service.Free: {
        const ptr = ctx.memory.readWord(ctx.cpu.sp);
        ctx.heap.deallocate(ptr);
        ctx.cpu.ax = 0;
        break;
      }
      case Int86Service.Calloc: {
        const count = ctx.memory.readWord(ctx.cpu.sp);
        const size = ctx.memory.readWord(ctx.cpu.sp + 2);
        ctx.cpu.ax = CpuHelper.toWord(ctx.heap.allocateZeroed(count, size));
        break;
      }
      case Int86Service.Realloc: {
        const ptr = ctx.memory.readWord(ctx.cpu.sp);
        const size = ctx.memory.readWord(ctx.cpu.sp + 2);
        ctx.cpu.ax = CpuHelper.toWord(ctx.heap.reallocate(ptr, size));
        break;
      }

      // ── stdlib.h conversion ────────────────────────────────────────
      case Int86Service.Itoa: {
        const value = DataHandler.toSigned16(ctx.memory.readWord(ctx.cpu.sp));
        const strAddr = ctx.memory.readWord(ctx.cpu.sp + 2);
        const base = ctx.memory.readWord(ctx.cpu.sp + 4);
        const str = DataHandler.intToString(value, base);
        for (let i = 0; i < str.length && strAddr + i < 0x10000; i++) {
          ctx.memory.writeByte(strAddr + i, str.charCodeAt(i));
        }
        ctx.memory.writeByte(strAddr + str.length, 0x24); // '$' terminator
        ctx.cpu.ax = CpuHelper.toWord(strAddr);
        break;
      }
      case Int86Service.Strtol: {
        const strAddr = ctx.memory.readWord(ctx.cpu.sp);
        // arg2 (endptr) is ignored in this simplified implementation
        const base = ctx.memory.readWord(ctx.cpu.sp + 4);
        let str = '';
        let k = 0;
        while (strAddr + k < 0x10000) {
          const ch = ctx.memory.readByte(strAddr + k);
          if (ch === 0 || ch === 0x24) break;
          str += String.fromCharCode(ch);
          k++;
        }
        const parsed = parseInt(str.trim(), base === 0 ? undefined : base);
        ctx.cpu.ax = CpuHelper.toWord(isNaN(parsed) ? 0 : parsed);
        break;
      }

      // ── time.h ─────────────────────────────────────────────────────
      case Int86Service.Time: {
        const ptr = ctx.memory.readWord(ctx.cpu.sp);
        const seconds = CpuHelper.toWord(Math.floor(Date.now() / 1000) & 0xFFFF);
        if (ptr !== 0 && ptr < 0x10000 - 1) {
          ctx.memory.writeWord(ptr, seconds);
        }
        ctx.cpu.ax = seconds;
        break;
      }
      case Int86Service.Clock: {
        const ticks = CpuHelper.toWord(Math.floor(performance.now()) & 0xFFFF);
        ctx.cpu.ax = ticks;
        break;
      }
      case Int86Service.Difftime: {
        const t2 = DataHandler.toSigned16(ctx.memory.readWord(ctx.cpu.sp));
        const t1 = DataHandler.toSigned16(ctx.memory.readWord(ctx.cpu.sp + 2));
        ctx.cpu.ax = CpuHelper.toWord(t2 - t1);
        break;
      }

      // ── stdio.h input (scanf support) ──────────────────────────────
      case Int86Service.ReadInt: {
        // Read an integer from stdin. Collects chars via onInput until a
        // non-digit/sign is encountered, parses the result, returns in AX.
        let numStr = '';
        if (ctx.onInput) {
          let first = true;
          while (true) {
            const ch = ctx.onInput();
            if (!ch || ch.length === 0) break;
            const code = ch.charCodeAt(0);
            if (first && (code === 0x2D || code === 0x2B)) {
              // leading sign
              numStr += ch[0];
              if (ctx.onOutput) ctx.onOutput(ch[0]);
              first = false;
              continue;
            }
            if (code >= 0x30 && code <= 0x39) {
              numStr += ch[0];
              if (ctx.onOutput) ctx.onOutput(ch[0]);
              first = false;
            } else {
              break;
            }
          }
        }
        const parsed = parseInt(numStr, 10);
        ctx.cpu.ax = CpuHelper.toWord(isNaN(parsed) ? 0 : parsed & 0xFFFF);
        break;
      }
      case Int86Service.ReadStr: {
        // Read a whitespace-delimited string from stdin into buffer at [SP].
        const buf = ctx.memory.readWord(ctx.cpu.sp);
        let offset = 0;
        if (ctx.onInput) {
          while (offset < 254) {
            const ch = ctx.onInput();
            if (!ch || ch.length === 0) break;
            const code = ch.charCodeAt(0);
            if (code === 0x0A || code === 0x0D || code === 0x20 || code === 0x09) break;
            ctx.memory.writeByte(buf + offset, code & 0xFF);
            if (ctx.onOutput) ctx.onOutput(ch[0]);
            offset++;
          }
        }
        // Null-terminate and $-terminate for compatibility
        ctx.memory.writeByte(buf + offset, 0x24);
        ctx.memory.writeByte(buf + offset + 1, 0);
        ctx.cpu.ax = CpuHelper.toWord(offset);
        break;
      }
    }
  }

  private static assertOperandCount(mnemonic: string, ops: Operand[], expected: number): void {
    if (ops.length !== expected) {
      throw new Error(`${mnemonic} expects ${expected} operand(s), got ${ops.length}`);
    }
  }

  /** Interpret a 16-bit unsigned value as signed. */
  private static toSigned16(value: number): number {
    return value >= 0x8000 ? value - 0x10000 : value;
  }

  /** Convert a signed integer to a string in the given base. */
  private static intToString(value: number, base: number): string {
    const clampedBase = (base >= 2 && base <= 36) ? base : 10;
    if (value < 0 && clampedBase === 10) {
      return '-' + Math.abs(value).toString(clampedBase);
    }
    // For non-decimal bases, treat as unsigned 16-bit
    const unsigned = value < 0 ? value + 0x10000 : value;
    return unsigned.toString(clampedBase);
  }
}
