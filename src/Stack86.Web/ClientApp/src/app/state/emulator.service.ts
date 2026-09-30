import { Service } from '@angular/core';
import { ExecutionEngine } from '../core/emulator/execution/execution-engine';

@Service()
export class EmulatorService {
  public readonly engine = new ExecutionEngine();
}
