import {
  CpuState,
  RegisterName,
  SegmentRegisterName,
  ByteShift,
  CpuHelper,
} from '../cpu/cpu.model';
import { Memory } from '../memory/memory.model';
import { Operand, OperandSize, OperandType } from './instruction.model';
import { ExecutionTrace, MemoryAccessSize, BusDirection } from '../execution/execution-trace.model';

export class OperandHelper {
  public static read(
    cpu: CpuState,
    memory: Memory,
    op: Operand,
    labels: Map<string, number>,
    trace?: ExecutionTrace,
  ): number {
    switch (op.type) {
      case OperandType.Immediate:
        return typeof op.value === 'number' ? op.value : parseInt(op.value as string, 10);

      case OperandType.Register: {
        const name = (op.value as string).toLowerCase();
        let value: number;
        if (CpuHelper.isRegister(name)) {
          value = cpu[name as RegisterName];
        } else if (CpuHelper.isSegmentRegister(name)) {
          value = cpu[name as SegmentRegisterName];
        } else if (CpuHelper.isSubRegister(name)) {
          const sub = CpuHelper.SUB_REGISTER_MAP[name];
          const parent = cpu[sub.parent];
          value = sub.shift === ByteShift.High ? CpuHelper.getHighByte(parent) : CpuHelper.getLowByte(parent);
        } else {
          throw new Error(`Unknown register: ${op.value}`);
        }
        if (trace) {
          trace.registersRead.push(name);
        }
        return value;
      }

      case OperandType.Memory: {
        const addr = OperandHelper.resolveAddress(cpu, op, labels, trace);
        const value = op.size === OperandSize.Byte ? memory.readByte(addr) : memory.readWord(addr);
        if (trace) {
          const size = op.size === OperandSize.Byte ? MemoryAccessSize.Byte : MemoryAccessSize.Word;
          trace.memoryReads.push({ address: addr, size, value });
          trace.busTransfers.push({ direction: BusDirection.Read, address: addr, value });
        }
        return value;
      }

      case OperandType.Label: {
        const addr = labels.get(op.value as string);
        if (addr === undefined) {
          if (op.value === '@DATA') {
            return 0;
          }
          throw new Error(`Undefined label: ${op.value}`);
        }
        return addr;
      }
    }
  }

  public static write(
    cpu: CpuState,
    memory: Memory,
    op: Operand,
    value: number,
    labels: Map<string, number>,
    trace?: ExecutionTrace,
  ): void {
    switch (op.type) {
      case OperandType.Register: {
        const name = (op.value as string).toLowerCase();
        if (CpuHelper.isRegister(name)) {
          cpu[name as RegisterName] = CpuHelper.toWord(value);
        } else if (CpuHelper.isSegmentRegister(name)) {
          cpu[name as SegmentRegisterName] = CpuHelper.toWord(value);
        } else if (CpuHelper.isSubRegister(name)) {
          const sub = CpuHelper.SUB_REGISTER_MAP[name];
          const current = cpu[sub.parent];
          cpu[sub.parent] = sub.shift === ByteShift.High
            ? CpuHelper.setHighByte(current, CpuHelper.toByte(value))
            : CpuHelper.setLowByte(current, CpuHelper.toByte(value));
        } else {
          throw new Error(`Unknown register: ${op.value}`);
        }
        if (trace) {
          trace.registersWritten.push(name);
        }
        return;
      }

      case OperandType.Memory: {
        const addr = OperandHelper.resolveAddress(cpu, op, labels, trace);
        if (op.size === OperandSize.Byte) {
          memory.writeByte(addr, CpuHelper.toByte(value));
        } else {
          memory.writeWord(addr, CpuHelper.toWord(value));
        }
        if (trace) {
          const size = op.size === OperandSize.Byte ? MemoryAccessSize.Byte : MemoryAccessSize.Word;
          trace.memoryWrites.push({ address: addr, size, value });
          trace.busTransfers.push({ direction: BusDirection.Write, address: addr, value });
        }
        return;
      }

      default:
        throw new Error(`Cannot write to operand type: ${op.type}`);
    }
  }

  public static size(op: Operand): OperandSize {
    if (op.size) return op.size;
    if (op.type === OperandType.Register) {
      const name = (op.value as string).toLowerCase();
      return CpuHelper.isSubRegister(name) ? OperandSize.Byte : OperandSize.Word;
    }
    return OperandSize.Word;
  }

  private static resolveAddress(
    cpu: CpuState,
    op: Operand,
    labels: Map<string, number>,
    trace?: ExecutionTrace,
  ): number {
    const val = op.value;
    let addr: number;

    if (typeof val === 'number') {
      addr = val;
    } else {
      const inner = val as string;
      const lowerInner = inner.toLowerCase();
      if (CpuHelper.isRegister(lowerInner)) {
        addr = cpu[lowerInner as RegisterName];
        if (trace) {
          trace.registersRead.push(lowerInner);
        }
      } else {
        const labelAddr = labels.get(inner);
        if (labelAddr !== undefined) {
          addr = labelAddr;
        } else {
          const parsed = parseInt(inner, 16);
          if (!isNaN(parsed)) {
            addr = parsed;
          } else {
            throw new Error(`Cannot resolve memory address: ${inner}`);
          }
        }
      }
    }

    if (op.offset !== undefined) {
      addr += op.offset;
    }

    if (trace) {
      trace.segmentUsed = trace.segmentUsed ?? 'ds';
    }

    return CpuHelper.toWord(addr);
  }
}
