import { describe, it, expect } from 'vitest';
import { CpuHelper } from '../../cpu/cpu.model';
import { Memory } from '../../memory/memory.model';
import { HeapAllocator } from '../../memory/heap-allocator';
import { ExecutionContext } from '../instruction.model';
import { DataHandler } from './data.handler';
import { Int86Service } from './int86-services';

/**
 * Builds an ExecutionContext pointing to SP=0xFFF0 with the given
 * service number in AH and args pushed right-to-left on the stack.
 * args[0] = first C argument (last pushed, lowest address).
 */
function makeCtx(service: Int86Service, args: number[]): ExecutionContext {
  const memory = new Memory();
  const cpu = CpuHelper.createInitialState();
  cpu.sp = 0xFFF0;
  // Push args (already in left-to-right order at ascending SP offsets)
  for (let i = 0; i < args.length; i++) {
    memory.writeWord(cpu.sp + i * 2, args[i] & 0xFFFF);
  }
  cpu.ax = (service << 8) & 0xFF00; // AH = service
  const heap = new HeapAllocator(memory);
  return { cpu, memory, labels: new Map(), halted: false, heap };
}

/** Write a $-terminated string into memory at the given address. */
function writeString(memory: Memory, addr: number, str: string): void {
  for (let i = 0; i < str.length; i++) {
    memory.writeByte(addr + i, str.charCodeAt(i));
  }
  memory.writeByte(addr + str.length, 0x24); // '$'
}

/** Read a $-terminated string from memory. */
function readString(memory: Memory, addr: number): string {
  let result = '';
  let a = addr;
  while (a < 0x10000) {
    const ch = memory.readByte(a);
    if (ch === 0x24 || ch === 0) break;
    result += String.fromCharCode(ch);
    a++;
  }
  return result;
}

function callInt86(ctx: ExecutionContext): void {
  DataHandler.int(ctx, [{ type: 'immediate' as never, value: 0x86 }]);
}

describe('INT 86h extended services', () => {
  // ── string.h ─────────────────────────────────────────────────────

  describe('Strncpy (0x0F)', () => {
    it('should copy at most n characters', () => {
      const ctx = makeCtx(Int86Service.Strncpy, [0x1000, 0x2000, 5]);
      writeString(ctx.memory, 0x2000, 'Hello, World');
      callInt86(ctx);
      expect(readString(ctx.memory, 0x1000)).toBe('Hello');
      expect(ctx.cpu.ax).toBe(0x1000);
    });
  });

  describe('Strncmp (0x10)', () => {
    it('should return 0 for equal prefixes', () => {
      const ctx = makeCtx(Int86Service.Strncmp, [0x1000, 0x2000, 3]);
      writeString(ctx.memory, 0x1000, 'Hello');
      writeString(ctx.memory, 0x2000, 'Help');
      callInt86(ctx);
      // First 3 chars: "Hel" vs "Hel" → 0
      expect(CpuHelper.toSigned16(ctx.cpu.ax)).toBe(0);
    });

    it('should return non-zero for different strings', () => {
      const ctx = makeCtx(Int86Service.Strncmp, [0x1000, 0x2000, 5]);
      writeString(ctx.memory, 0x1000, 'Hello');
      writeString(ctx.memory, 0x2000, 'Help');
      callInt86(ctx);
      // 'l' < 'p', so result < 0
      expect(CpuHelper.toSigned16(ctx.cpu.ax)).toBeLessThan(0);
    });
  });

  describe('Strncat (0x11)', () => {
    it('should append at most n chars', () => {
      const ctx = makeCtx(Int86Service.Strncat, [0x1000, 0x2000, 3]);
      writeString(ctx.memory, 0x1000, 'Hi');
      writeString(ctx.memory, 0x2000, 'Hello');
      callInt86(ctx);
      expect(readString(ctx.memory, 0x1000)).toBe('HiHel');
    });
  });

  describe('Strchr (0x12)', () => {
    it('should find the first occurrence of a character', () => {
      const ctx = makeCtx(Int86Service.Strchr, [0x1000, 0x6C]); // 'l'
      writeString(ctx.memory, 0x1000, 'Hello');
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0x1002); // 'l' at index 2
    });

    it('should return 0 when character not found', () => {
      const ctx = makeCtx(Int86Service.Strchr, [0x1000, 0x7A]); // 'z'
      writeString(ctx.memory, 0x1000, 'Hello');
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0);
    });
  });

  describe('Strrchr (0x13)', () => {
    it('should find the last occurrence of a character', () => {
      const ctx = makeCtx(Int86Service.Strrchr, [0x1000, 0x6C]); // 'l'
      writeString(ctx.memory, 0x1000, 'Hello');
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0x1003); // last 'l' at index 3
    });
  });

  describe('Strstr (0x14)', () => {
    it('should find a substring', () => {
      const ctx = makeCtx(Int86Service.Strstr, [0x1000, 0x2000]);
      writeString(ctx.memory, 0x1000, 'Hello World');
      writeString(ctx.memory, 0x2000, 'World');
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0x1006); // "World" starts at index 6
    });

    it('should return 0 when substring not found', () => {
      const ctx = makeCtx(Int86Service.Strstr, [0x1000, 0x2000]);
      writeString(ctx.memory, 0x1000, 'Hello');
      writeString(ctx.memory, 0x2000, 'xyz');
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0);
    });

    it('should return haystack for empty needle', () => {
      const ctx = makeCtx(Int86Service.Strstr, [0x1000, 0x2000]);
      writeString(ctx.memory, 0x1000, 'Hello');
      writeString(ctx.memory, 0x2000, ''); // empty
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0x1000);
    });
  });

  describe('Memcmp (0x15)', () => {
    it('should return 0 for equal regions', () => {
      const ctx = makeCtx(Int86Service.Memcmp, [0x1000, 0x2000, 4]);
      writeString(ctx.memory, 0x1000, 'ABCD');
      writeString(ctx.memory, 0x2000, 'ABCD');
      callInt86(ctx);
      expect(CpuHelper.toSigned16(ctx.cpu.ax)).toBe(0);
    });

    it('should return non-zero for different regions', () => {
      const ctx = makeCtx(Int86Service.Memcmp, [0x1000, 0x2000, 4]);
      writeString(ctx.memory, 0x1000, 'ABCD');
      writeString(ctx.memory, 0x2000, 'ABCE');
      callInt86(ctx);
      expect(CpuHelper.toSigned16(ctx.cpu.ax)).toBeLessThan(0);
    });
  });

  // ── ctype.h ──────────────────────────────────────────────────────

  describe('Isalpha (0x16)', () => {
    it('should return 1 for alphabetic chars', () => {
      const ctx = makeCtx(Int86Service.Isalpha, [0x41]); // 'A'
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(1);
    });

    it('should return 0 for non-alphabetic chars', () => {
      const ctx = makeCtx(Int86Service.Isalpha, [0x31]); // '1'
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0);
    });
  });

  describe('Isdigit (0x17)', () => {
    it('should return 1 for digits', () => {
      const ctx = makeCtx(Int86Service.Isdigit, [0x35]); // '5'
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(1);
    });

    it('should return 0 for non-digits', () => {
      const ctx = makeCtx(Int86Service.Isdigit, [0x41]); // 'A'
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0);
    });
  });

  describe('Isalnum (0x18)', () => {
    it('should return 1 for alphanumeric chars', () => {
      for (const ch of [0x41, 0x7A, 0x30, 0x39]) {
        const ctx = makeCtx(Int86Service.Isalnum, [ch]);
        callInt86(ctx);
        expect(ctx.cpu.ax).toBe(1);
      }
    });
  });

  describe('Isspace (0x19)', () => {
    it('should return 1 for whitespace', () => {
      for (const ch of [0x20, 0x09, 0x0A, 0x0D]) {
        const ctx = makeCtx(Int86Service.Isspace, [ch]);
        callInt86(ctx);
        expect(ctx.cpu.ax).toBe(1);
      }
    });
  });

  describe('Isupper (0x1A)', () => {
    it('should detect uppercase', () => {
      const ctx = makeCtx(Int86Service.Isupper, [0x41]); // 'A'
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(1);
    });
  });

  describe('Islower (0x1B)', () => {
    it('should detect lowercase', () => {
      const ctx = makeCtx(Int86Service.Islower, [0x61]); // 'a'
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(1);
    });
  });

  describe('Ispunct (0x1C)', () => {
    it('should detect punctuation', () => {
      const ctx = makeCtx(Int86Service.Ispunct, [0x21]); // '!'
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(1);
    });

    it('should not count alphanumeric as punctuation', () => {
      const ctx = makeCtx(Int86Service.Ispunct, [0x41]); // 'A'
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0);
    });
  });

  describe('Isprint (0x1D)', () => {
    it('should detect printable characters', () => {
      const ctx = makeCtx(Int86Service.Isprint, [0x41]); // 'A'
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(1);
    });

    it('should reject control characters', () => {
      const ctx = makeCtx(Int86Service.Isprint, [0x01]);
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0);
    });
  });

  describe('Isxdigit (0x1E)', () => {
    it('should detect hex digits', () => {
      for (const ch of [0x30, 0x39, 0x41, 0x46, 0x61, 0x66]) { // '0','9','A','F','a','f'
        const ctx = makeCtx(Int86Service.Isxdigit, [ch]);
        callInt86(ctx);
        expect(ctx.cpu.ax).toBe(1);
      }
    });
  });

  describe('Iscntrl (0x1F)', () => {
    it('should detect control characters', () => {
      const ctx = makeCtx(Int86Service.Iscntrl, [0x00]);
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(1);
    });
  });

  // ── stdlib.h heap ────────────────────────────────────────────────

  describe('Malloc / Free (0x20, 0x21)', () => {
    it('should allocate and free memory', () => {
      const ctx = makeCtx(Int86Service.Malloc, [32]);
      callInt86(ctx);
      const ptr = ctx.cpu.ax;
      expect(ptr).toBeGreaterThan(0);

      // Free it
      ctx.memory.writeWord(ctx.cpu.sp, ptr);
      ctx.cpu.ax = (Int86Service.Free << 8) & 0xFF00;
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0);
    });
  });

  describe('Calloc (0x22)', () => {
    it('should allocate zeroed memory', () => {
      const ctx = makeCtx(Int86Service.Calloc, [4, 4]);
      callInt86(ctx);
      const ptr = ctx.cpu.ax;
      expect(ptr).toBeGreaterThan(0);
      for (let i = 0; i < 16; i++) {
        expect(ctx.memory.readByte(ptr + i)).toBe(0);
      }
    });
  });

  describe('Realloc (0x23)', () => {
    it('should reallocate memory', () => {
      // First allocate
      const ctx = makeCtx(Int86Service.Malloc, [8]);
      callInt86(ctx);
      const ptr = ctx.cpu.ax;
      ctx.memory.writeByte(ptr, 0xAA);

      // Realloc to larger
      ctx.memory.writeWord(ctx.cpu.sp, ptr);
      ctx.memory.writeWord(ctx.cpu.sp + 2, 32);
      ctx.cpu.ax = (Int86Service.Realloc << 8) & 0xFF00;
      callInt86(ctx);
      const newPtr = ctx.cpu.ax;
      expect(newPtr).toBeGreaterThan(0);
      expect(ctx.memory.readByte(newPtr)).toBe(0xAA);
    });
  });

  // ── stdlib.h conversion ──────────────────────────────────────────

  describe('Itoa (0x24)', () => {
    it('should convert integer to decimal string', () => {
      const ctx = makeCtx(Int86Service.Itoa, [42, 0x3000, 10]);
      callInt86(ctx);
      expect(readString(ctx.memory, 0x3000)).toBe('42');
    });

    it('should handle negative numbers in base 10', () => {
      const ctx = makeCtx(Int86Service.Itoa, [CpuHelper.toWord(-5), 0x3000, 10]);
      callInt86(ctx);
      expect(readString(ctx.memory, 0x3000)).toBe('-5');
    });

    it('should convert to hex', () => {
      const ctx = makeCtx(Int86Service.Itoa, [255, 0x3000, 16]);
      callInt86(ctx);
      expect(readString(ctx.memory, 0x3000)).toBe('ff');
    });
  });

  describe('Strtol (0x25)', () => {
    it('should parse a decimal string', () => {
      const ctx = makeCtx(Int86Service.Strtol, [0x1000, 0, 10]);
      writeString(ctx.memory, 0x1000, '123');
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(123);
    });

    it('should parse a hex string', () => {
      const ctx = makeCtx(Int86Service.Strtol, [0x1000, 0, 16]);
      writeString(ctx.memory, 0x1000, 'FF');
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(255);
    });
  });

  // ── time.h ───────────────────────────────────────────────────────

  describe('Time (0x26)', () => {
    it('should return a non-zero timestamp', () => {
      const ctx = makeCtx(Int86Service.Time, [0]);
      callInt86(ctx);
      expect(ctx.cpu.ax).toBeGreaterThan(0);
    });

    it('should write timestamp to pointer if non-null', () => {
      const ctx = makeCtx(Int86Service.Time, [0x3000]);
      callInt86(ctx);
      const written = ctx.memory.readWord(0x3000);
      expect(written).toBe(ctx.cpu.ax);
    });
  });

  describe('Clock (0x27)', () => {
    it('should return a value', () => {
      const ctx = makeCtx(Int86Service.Clock, []);
      callInt86(ctx);
      // Just verify it doesn't crash and returns something
      expect(typeof ctx.cpu.ax).toBe('number');
    });
  });

  describe('Difftime (0x28)', () => {
    it('should compute the difference between two times', () => {
      const ctx = makeCtx(Int86Service.Difftime, [100, 90]);
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(10);
    });
  });

  // ── Integration: realistic C-like program scenarios ──────────────

  describe('Integration: malloc + string ops + ctype', () => {
    it('should malloc a buffer, strcpy into it, and verify with strlen and isalpha', () => {
      // 1. malloc(16)
      const ctx = makeCtx(Int86Service.Malloc, [16]);
      callInt86(ctx);
      const buf = ctx.cpu.ax;
      expect(buf).toBeGreaterThan(0);

      // 2. Write "Hello" to a source address
      writeString(ctx.memory, 0x1000, 'Hello');

      // 3. strcpy(buf, 0x1000)
      ctx.memory.writeWord(ctx.cpu.sp, buf);
      ctx.memory.writeWord(ctx.cpu.sp + 2, 0x1000);
      ctx.cpu.ax = (Int86Service.Strcpy << 8) & 0xFF00;
      callInt86(ctx);
      expect(readString(ctx.memory, buf)).toBe('Hello');

      // 4. strlen(buf) → 5
      ctx.memory.writeWord(ctx.cpu.sp, buf);
      ctx.cpu.ax = (Int86Service.Strlen << 8) & 0xFF00;
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(5);

      // 5. isalpha(buf[0]) → 1 ('H')
      ctx.memory.writeWord(ctx.cpu.sp, ctx.memory.readByte(buf));
      ctx.cpu.ax = (Int86Service.Isalpha << 8) & 0xFF00;
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(1);

      // 6. strchr(buf, 'l') → address of first 'l'
      ctx.memory.writeWord(ctx.cpu.sp, buf);
      ctx.memory.writeWord(ctx.cpu.sp + 2, 0x6C);
      ctx.cpu.ax = (Int86Service.Strchr << 8) & 0xFF00;
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(buf + 2);

      // 7. free(buf) — should not crash
      ctx.memory.writeWord(ctx.cpu.sp, buf);
      ctx.cpu.ax = (Int86Service.Free << 8) & 0xFF00;
      callInt86(ctx);
      expect(ctx.cpu.ax).toBe(0);
    });

    it('should calloc, memset, memcmp, and itoa in sequence', () => {
      // 1. calloc(1, 10) — 10 zeroed bytes
      const ctx = makeCtx(Int86Service.Calloc, [1, 10]);
      callInt86(ctx);
      const buf = ctx.cpu.ax;
      expect(buf).toBeGreaterThan(0);
      for (let i = 0; i < 10; i++) {
        expect(ctx.memory.readByte(buf + i)).toBe(0);
      }

      // 2. memset(buf, 'A', 5) — fill first 5 bytes with 'A'
      ctx.memory.writeWord(ctx.cpu.sp, buf);
      ctx.memory.writeWord(ctx.cpu.sp + 2, 0x41);
      ctx.memory.writeWord(ctx.cpu.sp + 4, 5);
      ctx.cpu.ax = (Int86Service.Memset << 8) & 0xFF00;
      callInt86(ctx);
      for (let i = 0; i < 5; i++) {
        expect(ctx.memory.readByte(buf + i)).toBe(0x41);
      }
      expect(ctx.memory.readByte(buf + 5)).toBe(0); // rest still zero

      // 3. Write "AAAAA" at 0x2000 and memcmp with buf
      writeString(ctx.memory, 0x2000, 'AAAAA');
      ctx.memory.writeWord(ctx.cpu.sp, buf);
      ctx.memory.writeWord(ctx.cpu.sp + 2, 0x2000);
      ctx.memory.writeWord(ctx.cpu.sp + 4, 5);
      ctx.cpu.ax = (Int86Service.Memcmp << 8) & 0xFF00;
      callInt86(ctx);
      expect(CpuHelper.toSigned16(ctx.cpu.ax)).toBe(0); // equal

      // 4. itoa(12345, 0x3000, 10) → "12345"
      ctx.memory.writeWord(ctx.cpu.sp, 12345);
      ctx.memory.writeWord(ctx.cpu.sp + 2, 0x3000);
      ctx.memory.writeWord(ctx.cpu.sp + 4, 10);
      ctx.cpu.ax = (Int86Service.Itoa << 8) & 0xFF00;
      callInt86(ctx);
      expect(readString(ctx.memory, 0x3000)).toBe('12345');

      // 5. free(buf)
      ctx.memory.writeWord(ctx.cpu.sp, buf);
      ctx.cpu.ax = (Int86Service.Free << 8) & 0xFF00;
      callInt86(ctx);
    });

    it('should use strstr to find a word, then strncat to build a new string', () => {
      // 1. strstr — find "brown" in the sentence
      const ctx = makeCtx(Int86Service.Strstr, [0x1000, 0x2000]);

      // Source strings
      writeString(ctx.memory, 0x1000, 'The quick brown fox');
      writeString(ctx.memory, 0x2000, 'brown');
      writeString(ctx.memory, 0x3000, 'Found: ');

      callInt86(ctx);
      const found = ctx.cpu.ax;
      expect(found).toBe(0x100A); // "brown" starts at offset 10

      // 2. strncat(0x3000, found, 5) → "Found: brown"
      ctx.memory.writeWord(ctx.cpu.sp, 0x3000);
      ctx.memory.writeWord(ctx.cpu.sp + 2, found);
      ctx.memory.writeWord(ctx.cpu.sp + 4, 5);
      ctx.cpu.ax = (Int86Service.Strncat << 8) & 0xFF00;
      callInt86(ctx);
      expect(readString(ctx.memory, 0x3000)).toBe('Found: brown');
    });
  });
});
