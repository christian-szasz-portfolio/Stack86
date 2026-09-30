/**
 * 8086 Memory model — 64KB address space, framework-agnostic.
 */

export class Memory {
  private readonly data: Uint8Array;

  public constructor() {
    this.data = new Uint8Array(0x10000); // 64KB
  }

  public readByte(address: number): number {
    this.assertAddress(address);
    return this.data[address];
  }

  public writeByte(address: number, value: number): void {
    this.assertAddress(address);
    this.data[address] = value & 0xFF;
  }

  /** Read a 16-bit word (little-endian, as per x86) */
  public readWord(address: number): number {
    this.assertAddress(address);
    this.assertAddress(address + 1);
    return this.data[address] | (this.data[address + 1] << 8);
  }

  /** Write a 16-bit word (little-endian) */
  public writeWord(address: number, value: number): void {
    this.assertAddress(address);
    this.assertAddress(address + 1);
    this.data[address] = value & 0xFF;
    this.data[address + 1] = (value >> 8) & 0xFF;
  }

  /** Load a byte array into memory starting at the given offset */
  public loadProgram(bytes: number[], offset: number = 0): void {
    if (offset < 0 || offset + bytes.length > this.data.length) {
      throw new RangeError(
        `Program of ${bytes.length} bytes at offset 0x${offset.toString(16)} exceeds 64KB address space`
      );
    }
    for (let i = 0; i < bytes.length; i++) {
      this.data[offset + i] = bytes[i] & 0xFF;
    }
  }

  /** Return a copy of a memory region */
  public slice(start: number, length: number): Uint8Array {
    this.assertAddress(start);
    const end = Math.min(start + length, this.data.length);
    return this.data.slice(start, end);
  }

  /** Return a snapshot of the full memory (copy) */
  public snapshot(): Uint8Array {
    return this.data.slice();
  }

  /** Return a reference to the raw buffer (for performance-sensitive reads) */
  public raw(): Uint8Array {
    return this.data;
  }

  public reset(): void {
    this.data.fill(0);
  }

  public get size(): number {
    return this.data.length;
  }

  private assertAddress(address: number): void {
    if (address < 0 || address >= this.data.length) {
      throw new RangeError(`Memory access out of bounds: 0x${address.toString(16)}`);
    }
  }
}
