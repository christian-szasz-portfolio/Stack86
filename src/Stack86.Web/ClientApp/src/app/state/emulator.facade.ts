import { Service, inject, signal } from '@angular/core';
import { Store } from '@ngrx/store';
import { EmulatorActions } from './emulator.actions';
import {
  selectCpu,
  selectRegisters,
  selectFlags,
  selectMemory,
  selectInstructions,
  selectCurrentInstruction,
  selectExecutionStatus,
  selectBreakpoints,
  selectConsoleOutput,
  selectError,
  selectSourceCode,
  selectLabels,
  selectStackView,
  selectMemorySlice,
  selectParseErrors,
  selectLastTrace,
} from './emulator.selectors';

@Service()
export class EmulatorFacade {
  private readonly store = inject(Store);

  public readonly cpu = this.store.selectSignal(selectCpu);
  public readonly registers = this.store.selectSignal(selectRegisters);
  public readonly flags = this.store.selectSignal(selectFlags);
  public readonly memory = this.store.selectSignal(selectMemory);
  public readonly instructions = this.store.selectSignal(selectInstructions);
  public readonly currentInstruction = this.store.selectSignal(selectCurrentInstruction);
  public readonly executionStatus = this.store.selectSignal(selectExecutionStatus);
  public readonly breakpoints = this.store.selectSignal(selectBreakpoints);
  public readonly consoleOutput = this.store.selectSignal(selectConsoleOutput);
  public readonly error = this.store.selectSignal(selectError);
  public readonly sourceCode = this.store.selectSignal(selectSourceCode);
  public readonly labels = this.store.selectSignal(selectLabels);
  public readonly stackView = this.store.selectSignal(selectStackView);
  public readonly parseErrors = this.store.selectSignal(selectParseErrors);
  public readonly lastTrace = this.store.selectSignal(selectLastTrace);

  /** Execution delay in milliseconds. 0 = full speed (batched). >0 = animated step-by-step. */
  public readonly executionDelayMs = signal(0);

  public memorySlice(start: number, length: number) {
    return this.store.selectSignal(selectMemorySlice(start, length));
  }

  public loadProgram(source: string): void {
    this.store.dispatch(EmulatorActions.loadProgram({ source }));
  }

  public step(): void {
    this.store.dispatch(EmulatorActions.step());
  }

  public run(): void {
    this.store.dispatch(EmulatorActions.run());
  }

  public pause(): void {
    this.store.dispatch(EmulatorActions.pause());
  }

  public reset(): void {
    this.store.dispatch(EmulatorActions.reset());
  }

  public setBreakpoint(address: number): void {
    this.store.dispatch(EmulatorActions.setBreakpoint({ address }));
  }

  public removeBreakpoint(address: number): void {
    this.store.dispatch(EmulatorActions.removeBreakpoint({ address }));
  }

  public updateSourceCode(source: string): void {
    this.store.dispatch(EmulatorActions.updateSourceCode({ source }));
  }

  public setExecutionDelay(ms: number): void {
    this.executionDelayMs.set(ms);
  }

  public exportCom(): void {
    this.store.dispatch(EmulatorActions.exportCom());
  }

  public importCom(): void {
    this.store.dispatch(EmulatorActions.importCom());
  }

  public exportAsm(): void {
    this.store.dispatch(EmulatorActions.exportAsm());
  }

  public importAsm(): void {
    this.store.dispatch(EmulatorActions.importAsm());
  }
}
