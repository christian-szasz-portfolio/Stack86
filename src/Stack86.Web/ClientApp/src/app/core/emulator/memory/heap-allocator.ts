import { Memory } from './memory.model';

/**
 * Block header layout (4 bytes):
 *   [0..1] size   — usable payload size in bytes (excludes header)
 *   [2..3] flags  — bit 0 = free (1) / allocated (0)
 *
 * Blocks are stored contiguously. The next block starts at
 * `blockAddr + HEADER_SIZE + payloadSize`.
 */
const HEADER_SIZE = 4;
const FLAG_FREE = 1;

/**
 * Simple first-fit heap allocator operating within a reserved region
 * of the emulator's 64KB memory space.
 *
 * Default region: 0x8000–0xEFFF (28 KB).
 * Each block carries a 4-byte header (2B size + 2B flags).
 */
export class HeapAllocator {
  private readonly heapStart: number;
  private readonly heapEnd: number;
  private initialized = false;

  public constructor(
    private readonly memory: Memory,
    heapStart = 0x8000,
    heapEnd = 0xF000,
  ) {
    this.heapStart = heapStart;
    this.heapEnd = heapEnd;
  }

  /** Allocate `size` bytes. Returns the payload address or 0 on failure. */
  public allocate(size: number): number {
    if (size <= 0) return 0;
    this.ensureInitialized();

    // Align to 2 bytes for word-aligned access
    const alignedSize = (size + 1) & ~1;
    let addr = this.heapStart;

    while (addr + HEADER_SIZE <= this.heapEnd) {
      const blockSize = this.memory.readWord(addr);
      const flags = this.memory.readWord(addr + 2);

      if ((flags & FLAG_FREE) !== 0 && blockSize >= alignedSize) {
        // Split if remaining space can hold another block
        const remaining = blockSize - alignedSize;
        if (remaining >= HEADER_SIZE + 2) {
          // Shrink current block
          this.memory.writeWord(addr, alignedSize);
          // Create free block after this one
          const nextBlock = addr + HEADER_SIZE + alignedSize;
          this.memory.writeWord(nextBlock, remaining - HEADER_SIZE);
          this.memory.writeWord(nextBlock + 2, FLAG_FREE);
        }
        // Mark as allocated
        this.memory.writeWord(addr + 2, 0);
        return addr + HEADER_SIZE;
      }

      // Move to next block
      addr += HEADER_SIZE + blockSize;
    }

    return 0; // Out of memory
  }

  /** Free a previously allocated block. */
  public deallocate(ptr: number): void {
    if (ptr === 0) return;
    const blockAddr = ptr - HEADER_SIZE;
    if (blockAddr < this.heapStart || blockAddr >= this.heapEnd) return;

    // Mark as free
    this.memory.writeWord(blockAddr + 2, FLAG_FREE);

    // Coalesce with next block if it's also free
    this.coalesceForward(blockAddr);

    // Coalesce with previous block by scanning from start
    this.coalescePrevious(blockAddr);
  }

  /** Allocate zeroed memory for `count * size` bytes. */
  public allocateZeroed(count: number, size: number): number {
    const total = count * size;
    const ptr = this.allocate(total);
    if (ptr === 0) return 0;
    for (let i = 0; i < total && ptr + i < this.heapEnd; i++) {
      this.memory.writeByte(ptr + i, 0);
    }
    return ptr;
  }

  /** Reallocate a block to a new size, preserving existing data. */
  public reallocate(ptr: number, newSize: number): number {
    if (ptr === 0) return this.allocate(newSize);
    if (newSize === 0) {
      this.deallocate(ptr);
      return 0;
    }

    const blockAddr = ptr - HEADER_SIZE;
    if (blockAddr < this.heapStart || blockAddr >= this.heapEnd) return 0;

    const oldSize = this.memory.readWord(blockAddr);

    // If the block is already large enough, keep it
    const alignedNew = (newSize + 1) & ~1;
    if (oldSize >= alignedNew) return ptr;

    // Allocate new, copy, free old
    const newPtr = this.allocate(newSize);
    if (newPtr === 0) return 0;

    const copyLen = Math.min(oldSize, newSize);
    for (let i = 0; i < copyLen; i++) {
      this.memory.writeByte(newPtr + i, this.memory.readByte(ptr + i));
    }
    this.deallocate(ptr);
    return newPtr;
  }

  /** Reset heap to initial (single free block) state. */
  public reset(): void {
    this.initialized = false;
  }

  private ensureInitialized(): void {
    if (this.initialized) return;
    const totalPayload = this.heapEnd - this.heapStart - HEADER_SIZE;
    this.memory.writeWord(this.heapStart, totalPayload);
    this.memory.writeWord(this.heapStart + 2, FLAG_FREE);
    this.initialized = true;
  }

  private coalesceForward(blockAddr: number): void {
    const blockSize = this.memory.readWord(blockAddr);
    const nextAddr = blockAddr + HEADER_SIZE + blockSize;
    if (nextAddr + HEADER_SIZE > this.heapEnd) return;

    const nextFlags = this.memory.readWord(nextAddr + 2);
    if ((nextFlags & FLAG_FREE) !== 0) {
      const nextSize = this.memory.readWord(nextAddr);
      // Merge: absorb next block's header + payload into current
      this.memory.writeWord(blockAddr, blockSize + HEADER_SIZE + nextSize);
    }
  }

  private coalescePrevious(targetAddr: number): void {
    let addr = this.heapStart;
    while (addr + HEADER_SIZE <= this.heapEnd && addr < targetAddr) {
      const blockSize = this.memory.readWord(addr);
      const nextAddr = addr + HEADER_SIZE + blockSize;
      if (nextAddr === targetAddr) {
        const flags = this.memory.readWord(addr + 2);
        if ((flags & FLAG_FREE) !== 0) {
          // Previous block is free — merge current into it
          this.coalesceForward(addr);
        }
        return;
      }
      addr = nextAddr;
    }
  }
}
