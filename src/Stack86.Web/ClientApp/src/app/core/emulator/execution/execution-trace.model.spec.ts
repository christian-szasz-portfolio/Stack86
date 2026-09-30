import { describe, expect, it } from 'vitest';
import {
  BusDirection,
  ExecutionTraceFactory,
  IoType,
  MemoryAccessSize,
  StackAccessType,
} from './execution-trace.model';

describe('ExecutionTraceFactory', () => {
  it('creates a fresh, empty trace', () => {
    const trace = ExecutionTraceFactory.create();
    expect(trace.registersRead).toEqual([]);
    expect(trace.registersWritten).toEqual([]);
    expect(trace.memoryReads).toEqual([]);
    expect(trace.memoryWrites).toEqual([]);
    expect(trace.aluUsed).toBe(false);
    expect(trace.ioType).toBeNull();
    expect(trace.stackAccess).toBeNull();
    expect(trace.busTransfers).toEqual([]);
    expect(trace.segmentUsed).toBeNull();
  });

  it('returns a new instance each call (no shared state)', () => {
    const a = ExecutionTraceFactory.create();
    const b = ExecutionTraceFactory.create();
    a.registersRead.push('AX');
    expect(b.registersRead).toEqual([]);
  });
});

describe('execution trace enums', () => {
  it('exposes MemoryAccessSize values', () => {
    expect(MemoryAccessSize.Byte).toBe('byte');
    expect(MemoryAccessSize.Word).toBe('word');
  });

  it('exposes BusDirection values', () => {
    expect(BusDirection.Read).toBe('read');
    expect(BusDirection.Write).toBe('write');
  });

  it('exposes IoType values', () => {
    expect(IoType.Input).toBe('input');
    expect(IoType.Output).toBe('output');
  });

  it('exposes StackAccessType values', () => {
    expect(StackAccessType.Push).toBe('push');
    expect(StackAccessType.Pop).toBe('pop');
    expect(StackAccessType.Call).toBe('call');
    expect(StackAccessType.Ret).toBe('ret');
  });
});
