export enum MemoryAccessSize {
  Byte = 'byte',
  Word = 'word',
}

export enum BusDirection {
  Read = 'read',
  Write = 'write',
}

export enum IoType {
  Input = 'input',
  Output = 'output',
}

export enum StackAccessType {
  Push = 'push',
  Pop = 'pop',
  Call = 'call',
  Ret = 'ret',
}

export interface MemoryAccess {
  address: number;
  size: MemoryAccessSize;
  value: number;
}

export interface BusTransfer {
  direction: BusDirection;
  address: number;
  value: number;
}

export interface ExecutionTrace {
  registersRead: string[];
  registersWritten: string[];
  memoryReads: MemoryAccess[];
  memoryWrites: MemoryAccess[];
  aluUsed: boolean;
  ioType: IoType | null;
  stackAccess: StackAccessType | null;
  busTransfers: BusTransfer[];
  segmentUsed: string | null;
}

export class ExecutionTraceFactory {
  public static create(): ExecutionTrace {
    return {
      registersRead: [],
      registersWritten: [],
      memoryReads: [],
      memoryWrites: [],
      aluUsed: false,
      ioType: null,
      stackAccess: null,
      busTransfers: [],
      segmentUsed: null,
    };
  }
}
