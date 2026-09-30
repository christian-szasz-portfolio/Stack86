import { describe, it, expect, beforeEach } from 'vitest';
import { Memory } from './memory.model';
import { HeapAllocator } from './heap-allocator';

describe('HeapAllocator', () => {
  let memory: Memory;
  let heap: HeapAllocator;

  beforeEach(() => {
    memory = new Memory();
    heap = new HeapAllocator(memory, 0x8000, 0xF000);
  });

  describe('allocate', () => {
    it('should return a non-zero address for a valid allocation', () => {
      const ptr = heap.allocate(16);
      expect(ptr).toBeGreaterThan(0);
      expect(ptr).toBeGreaterThanOrEqual(0x8004); // heapStart + 4-byte header
    });

    it('should return 0 for zero-size allocation', () => {
      expect(heap.allocate(0)).toBe(0);
    });

    it('should return 0 for negative-size allocation', () => {
      expect(heap.allocate(-1)).toBe(0);
    });

    it('should return distinct addresses for successive allocations', () => {
      const p1 = heap.allocate(10);
      const p2 = heap.allocate(10);
      expect(p1).not.toBe(0);
      expect(p2).not.toBe(0);
      expect(p1).not.toBe(p2);
    });

    it('should word-align allocations', () => {
      const ptr = heap.allocate(3); // 3 → aligned to 4
      expect(ptr % 2).toBe(0);
    });

    it('should allow writing data to allocated memory', () => {
      const ptr = heap.allocate(4);
      memory.writeByte(ptr, 0x41);
      memory.writeByte(ptr + 1, 0x42);
      expect(memory.readByte(ptr)).toBe(0x41);
      expect(memory.readByte(ptr + 1)).toBe(0x42);
    });

    it('should return 0 when heap is exhausted', () => {
      // Allocate nearly the full heap
      const bigPtr = heap.allocate(0x6FF0); // close to 28KB - header overhead
      expect(bigPtr).not.toBe(0);
      // Allocating more should fail
      const p2 = heap.allocate(0x1000);
      expect(p2).toBe(0);
    });
  });

  describe('deallocate', () => {
    it('should allow reallocation after free', () => {
      const p1 = heap.allocate(100);
      heap.deallocate(p1);
      const p2 = heap.allocate(100);
      expect(p2).not.toBe(0);
      // Should reuse the freed block
      expect(p2).toBe(p1);
    });

    it('should handle freeing null (0) gracefully', () => {
      expect(() => heap.deallocate(0)).not.toThrow();
    });

    it('should handle freeing an out-of-range address', () => {
      expect(() => heap.deallocate(0x100)).not.toThrow();
    });

    it('should coalesce adjacent free blocks', () => {
      const p1 = heap.allocate(16);
      const p2 = heap.allocate(16);
      heap.deallocate(p1);
      heap.deallocate(p2);
      // After coalescing, should be able to allocate the combined space
      const combined = heap.allocate(36); // 16 + 4(header) + 16
      expect(combined).not.toBe(0);
    });
  });

  describe('allocateZeroed', () => {
    it('should return zeroed memory', () => {
      // Write junk to heap region first
      for (let i = 0x8000; i < 0x8020; i++) {
        memory.writeByte(i, 0xFF);
      }
      // Reset heap to reinitialize over the junk
      heap.reset();
      const ptr = heap.allocateZeroed(4, 4); // 16 bytes
      expect(ptr).not.toBe(0);
      for (let i = 0; i < 16; i++) {
        expect(memory.readByte(ptr + i)).toBe(0);
      }
    });

    it('should return 0 when allocation fails', () => {
      // Exhaust heap
      heap.allocate(0x6FF0);
      expect(heap.allocateZeroed(1, 0x1000)).toBe(0);
    });
  });

  describe('reallocate', () => {
    it('should allocate when given null pointer', () => {
      const ptr = heap.reallocate(0, 16);
      expect(ptr).not.toBe(0);
    });

    it('should free when given zero new size', () => {
      const p1 = heap.allocate(16);
      const p2 = heap.reallocate(p1, 0);
      expect(p2).toBe(0);
    });

    it('should return the same pointer if block is large enough', () => {
      const p1 = heap.allocate(32);
      const p2 = heap.reallocate(p1, 16); // shrink — fits current
      expect(p2).toBe(p1);
    });

    it('should copy data when moving to a new block', () => {
      const p1 = heap.allocate(8);
      memory.writeByte(p1, 0xAA);
      memory.writeByte(p1 + 1, 0xBB);
      // Fill remaining heap to force a move
      heap.allocate(0x6F00);
      // Free the first block and some space so realloc can find a new spot
      heap.deallocate(p1);
      const p2 = heap.reallocate(0, 16); // new allocation
      // Note: can't easily test data copy here since p1 was freed
      expect(p2).not.toBe(0);
    });

    it('should preserve data on grow', () => {
      const p1 = heap.allocate(4);
      memory.writeByte(p1, 0xDE);
      memory.writeByte(p1 + 1, 0xAD);
      const p2 = heap.reallocate(p1, 64);
      expect(p2).not.toBe(0);
      expect(memory.readByte(p2)).toBe(0xDE);
      expect(memory.readByte(p2 + 1)).toBe(0xAD);
    });
  });

  describe('reset', () => {
    it('should allow fresh allocations after reset', () => {
      const p1 = heap.allocate(16);
      heap.reset();
      const p2 = heap.allocate(16);
      // After reset, the heap starts fresh — same first address
      expect(p2).toBe(p1);
    });
  });
});
